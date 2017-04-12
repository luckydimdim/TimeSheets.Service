using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AutoMapper;
using Cmas.BusinessLayers.CallOffOrders;
using Cmas.BusinessLayers.CallOffOrders.Entities;
using Cmas.BusinessLayers.Contracts;
using Cmas.BusinessLayers.TimeSheets;
using Cmas.BusinessLayers.TimeSheets.Entities;
using Cmas.Infrastructure.Domain.Commands;
using Cmas.Infrastructure.Domain.Queries;
using Cmas.Services.TimeSheets.Dtos.Requests;
using Cmas.Services.TimeSheets.Dtos.Responses;
using Cmas.Services.TimeSheets.Dtos.Responses.AdditionalData;
using Cmas.Services.TimeSheets.Dtos.Responses.Rate;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Validation;
using System.IO;

namespace Cmas.Services.TimeSheets
{
    public static class ConverterHelper
    {
        public static RateResponse ConvertToTimeSheetRate(this Rate callOffOrderRate,
            Dictionary<string, IEnumerable<double>> spentTime)
        {
            var result = new RateResponse();
            string rateId = callOffOrderRate.Id.ToString();

            result.Name = callOffOrderRate.Name;
            result.Id = callOffOrderRate.Id.ToString();

            if (spentTime != null && spentTime.ContainsKey(rateId))
            {
                result.SpentTime = spentTime[rateId].ToList();
            }

            return result;
        }
    }

    public class RequestsModule : NancyModule
    {
        private readonly ICommandBuilder _commandBuilder;
        private readonly IQueryBuilder _queryBuilder;
        private readonly CallOffOrdersBusinessLayer _callOffOrdersBusinessLayer;
        private readonly ContractBusinessLayer _contractBusinessLayer;
        private readonly TimeSheetsBusinessLayer _timeSheetsBusinessLayer;
        private readonly IMapper _autoMapper;

        private async Task<DetailedTimeSheetResponse> GetDetailedTimeSheetAsync(string timeSheetId)
        {
            var timeSheet = await _timeSheetsBusinessLayer.GetTimeSheet(timeSheetId);

            return await GetDetailedTimeSheetAsync(timeSheet);
        }

        private async Task<DetailedTimeSheetResponse> GetDetailedTimeSheetAsync(TimeSheet timeSheet)
        {
            CallOffOrder callOffOrder = await _callOffOrdersBusinessLayer.GetCallOffOrder(timeSheet.CallOffOrderId);

            return GetDetailedTimeSheet(timeSheet, callOffOrder);
        }

        private DetailedTimeSheetResponse GetDetailedTimeSheet(TimeSheet timeSheet, CallOffOrder callOffOrder)
        {
            DetailedTimeSheetResponse result = _autoMapper.Map<DetailedTimeSheetResponse>(timeSheet);

            result.WorkName = callOffOrder.Name;

            if (callOffOrder.TemplateSysName.ToLower() == "default")
            {
                result.AdditionalData = new DefaultAdditionalDataResponse();
            }
            else if (callOffOrder.TemplateSysName.ToLower() == "southtambey")
            {
                result.AdditionalData = new SouthTambeyAdditionalDataResponse();

                var additionalData = (result.AdditionalData as SouthTambeyAdditionalDataResponse);
                additionalData.EmployeeNumber = callOffOrder.EmployeeNumber;
                additionalData.MobDate = callOffOrder.MobDate;
                additionalData.MobPlanReference = callOffOrder.MobPlanReference;
                additionalData.Paaf = callOffOrder.Paaf;
                additionalData.PositionNumber = callOffOrder.PositionNumber;
                additionalData.PersonnelSource = callOffOrder.PersonnelSource;
            }

            result.AdditionalData.Assignee = callOffOrder.Assignee;
            result.AdditionalData.Position = callOffOrder.Position;

            // проходим по ставкам наряд заказа
            foreach (var callOffOrderRate in callOffOrder.Rates)
            {
                if (!callOffOrderRate.IsRate) // группа
                {
                    var timeSheetRateGroup = new RateGroupResponse();
                    timeSheetRateGroup.Name = callOffOrderRate.Name;

                    var groupRates =
                        callOffOrder.Rates.Where(r => (r.IsRate && r.ParentId == callOffOrderRate.Id)).Select(r => r);


                    foreach (var groupRate in groupRates)
                    {
                        var timeSheetRate =
                            groupRate.ConvertToTimeSheetRate(timeSheet.SpentTime);

                        timeSheetRateGroup.Rates.Add(timeSheetRate);
                    }

                    result.RateGroups.Add(timeSheetRateGroup);
                }
                else if (callOffOrderRate.IsRate && !callOffOrderRate.ParentId.HasValue)
                {
                    var timeSheetRateGroup = new RateGroupResponse();

                    var timeSheetRate = callOffOrderRate.ConvertToTimeSheetRate(timeSheet.SpentTime);
                    timeSheetRateGroup.Rates.Add(timeSheetRate);

                    result.RateGroups.Add(timeSheetRateGroup);
                }
            }

            return result;
        }

        private async Task<IEnumerable<SimpleTimeSheetResponse>> GetSimpleTimeSheetsAsync(
            IEnumerable<TimeSheet> timeSheets)
        {
            var result = new List<SimpleTimeSheetResponse>();

            foreach (var timeSheet in timeSheets)
            {
                CallOffOrder callOffOrder = await _callOffOrdersBusinessLayer.GetCallOffOrder(timeSheet.CallOffOrderId);
                SimpleTimeSheetResponse simpleTimeSheet = _autoMapper.Map<SimpleTimeSheetResponse>(timeSheet);

                simpleTimeSheet.Assignee = callOffOrder.Assignee;
                simpleTimeSheet.Position = callOffOrder.Position;
                simpleTimeSheet.WorkName = callOffOrder.Name;
                simpleTimeSheet.CallOffOrderId = callOffOrder.Id;

                result.Add(simpleTimeSheet);
            }

            return result;
        }

        public RequestsModule(ICommandBuilder commandBuilder, IQueryBuilder queryBuilder, IMapper autoMapper)
            : base("/time-sheets")
        {
            _autoMapper = autoMapper;
            _commandBuilder = commandBuilder;
            _queryBuilder = queryBuilder;

            _callOffOrdersBusinessLayer = new CallOffOrdersBusinessLayer(_commandBuilder, _queryBuilder);
            _contractBusinessLayer = new ContractBusinessLayer(_commandBuilder, _queryBuilder);
            _timeSheetsBusinessLayer = new TimeSheetsBusinessLayer(_commandBuilder, _queryBuilder);

            /// <summary>
            /// /time-sheets/ - получить список табелей
            /// /time-sheets?callOffOrderId={id} - получить табели по указанному договору
            /// </summary>
            Get("/", async (args, ct) =>
            {
                string callOffOrderId = Request.Query["callOffOrderId"];

                IEnumerable<TimeSheet> result = null;

                if (callOffOrderId == null)
                    result = await _timeSheetsBusinessLayer.GetTimeSheets();
                else
                    result = await _timeSheetsBusinessLayer.GetTimeSheetsByCallOffOrderId(callOffOrderId);

                return await GetSimpleTimeSheetsAsync(result);
            });

            /// <summary>
            /// /time-sheets/{id} - получить табель по указанному ID
            /// </summary>
            /// <return>DetailedTimeSheetDto</return>
            Get("/{id}", async args => { return await GetDetailedTimeSheetAsync(args.id); });

            /// <summary>
            /// Создать табель учета рабочего времени
            /// </summary>
            /// <return>DetailedTimeSheetDto</return>
            Post("/", async (args, ct) =>
            {
                CreateTimeSheetRequest request = this.Bind<CreateTimeSheetRequest>();

                var validationResult = this.Validate(request);

                if (!validationResult.IsValid)
                {
                    return Negotiate.WithModel(validationResult).WithStatusCode(HttpStatusCode.BadRequest);
                }

                CallOffOrder callOffOrder = await _callOffOrdersBusinessLayer.GetCallOffOrder(request.CallOffOrderId);
                IEnumerable<TimeSheet> timeSheets =
                    await _timeSheetsBusinessLayer.GetTimeSheetsByCallOffOrderId(request.CallOffOrderId);

                // FIXME: Изменить после преобразования из string в DateTime
                DateTime startDate = DateTime.Parse(callOffOrder.StartDate).ToUniversalTime();

                // FIXME: Изменить после преобразования из string в DateTime
                DateTime finishDate = DateTime.Parse(callOffOrder.FinishDate).ToUniversalTime();


                string timeSheetId = null;
                bool created = false;
                while (startDate < finishDate)
                {
                    var tsExist =
                        timeSheets.Where(
                            ts =>
                                (ts.Month == startDate.Month &&
                                 ts.Year == startDate.Year)).Any();

                    if (tsExist)
                    {
                        startDate = startDate.AddMonths(1);
                        continue;
                    }
                    else
                    {
                        timeSheetId = await _timeSheetsBusinessLayer.CreateTimeSheet(request.CallOffOrderId,
                            startDate.Month, startDate.Year);
                        created = true;
                        break;
                    }
                }

                if (!created)
                {
                    timeSheetId = await _timeSheetsBusinessLayer.CreateTimeSheet(request.CallOffOrderId,
                        finishDate.Month, finishDate.Year);
                }

                return await GetDetailedTimeSheetAsync(timeSheetId);
            });

            /// <summary>
            /// Обновить отработанное время по работам
            /// </summary>
            /// <return>DetailedTimeSheetDto</return>
            Put("/{id}/spent-time", async args =>
            {
                TimeSheet timeSheet = await _timeSheetsBusinessLayer.GetTimeSheet(args.id);

                CallOffOrder callOffOrder = await _callOffOrdersBusinessLayer.GetCallOffOrder(timeSheet.CallOffOrderId);

                var requests = this.Bind<IEnumerable<UpdateTimesRequest>>();

                timeSheet.Amount = 0;
                foreach (var request in requests)
                {
                    var rateId = request.RateId;

                    Rate callOffOrderRate = callOffOrder.Rates.Where(r => r.Id.ToString() == rateId).SingleOrDefault();

                    if (callOffOrderRate == null)
                        throw new ArgumentException("rate not found: " + rateId);

                    if (!callOffOrderRate.IsRate)
                        throw new ArgumentException(String.Format("rate {0} is group", rateId));

                    timeSheet.Amount += TimeSheetsBusinessLayer.GetAmount(callOffOrderRate.Amount,
                        ConvertToTimeUnit(callOffOrderRate.UnitName), request.SpentTime);

                    timeSheet.SpentTime[request.RateId] = request.SpentTime;
                }

                await _timeSheetsBusinessLayer.UpdateTimeSheet(timeSheet);

                return Negotiate.WithStatusCode(HttpStatusCode.OK);
            });

            /// <summary>
            /// Обновить примечание
            /// </summary>
            /// <return>DetailedTimeSheetDto</return>
            Put("/{id}/notes", async args =>
            {
                var reader = new StreamReader(Request.Body);
                string notes = reader.ReadToEnd();

                TimeSheet timeSheet = await _timeSheetsBusinessLayer.GetTimeSheet(args.id);

                timeSheet.Notes = notes;

                await _timeSheetsBusinessLayer.UpdateTimeSheet(timeSheet);

                return Negotiate.WithStatusCode(HttpStatusCode.OK);
            });

            /// <summary>
            /// Обновить период (месяц, год)
            /// </summary>
            /// <return>DetailedTimeSheetDto</return>
            Put("/{id}/period", async args =>
            {
                UpdatePeriodRequest period = this.Bind<UpdatePeriodRequest>();

                TimeSheet timeSheet = await _timeSheetsBusinessLayer.GetTimeSheet(args.id);

                timeSheet.Month = period.Month;
                timeSheet.Year = period.Year;

                await _timeSheetsBusinessLayer.UpdateTimeSheet(timeSheet);

                return Negotiate.WithStatusCode(HttpStatusCode.OK);
            });
        }

        private TimeUnit ConvertToTimeUnit(string timeStr)
        {
            timeStr = timeStr.ToLower();

            if (timeStr.Contains("час"))
                return TimeUnit.Hour;
            else if (timeStr.Contains("ден"))
                return TimeUnit.Day;
            else
                return TimeUnit.None;
        }
    }
}