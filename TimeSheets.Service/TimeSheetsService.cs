using AutoMapper;
using Cmas.BusinessLayers.CallOffOrders;
using Cmas.BusinessLayers.Contracts;
using Cmas.BusinessLayers.TimeSheets;
using Cmas.Infrastructure.Domain.Commands;
using Cmas.Infrastructure.Domain.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cmas.BusinessLayers.CallOffOrders.Entities;
using Cmas.BusinessLayers.TimeSheets.Entities;
using Cmas.Infrastructure.ErrorHandler;
using Cmas.Services.TimeSheets.Dtos.Responses;
using Cmas.Services.TimeSheets.Dtos.Responses.AdditionalData;
using Cmas.Services.TimeSheets.Dtos.Responses.Rate;

namespace Cmas.Services.TimeSheets
{
    public static class ConverterHelper
    {
        /// <summary>
        /// Сконвертировать ставку из наряд заказа в ставку табеля
        /// </summary>
        /// <param name="callOffOrderRate">Ставка из наряд заказа</param>
        /// <param name="spentTime">Массив потраченного времени по даннйо ставке</param>
        /// <returns>Ставка с потраченным временем для табеля</returns>
        public static RateResponse ConvertToTimeSheetRate(this Rate callOffOrderRate,
            Dictionary<string, IEnumerable<double>> spentTime)
        {
            var result = new RateResponse();
            var rateId = callOffOrderRate.Id.ToString();

            result.Name = callOffOrderRate.Name;
            result.Id = callOffOrderRate.Id.ToString();

            if (spentTime != null && spentTime.ContainsKey(rateId))
                result.SpentTime = spentTime[rateId].ToList();

            return result;
        }
    }

    public class TimeSheetsService
    {
        private readonly CallOffOrdersBusinessLayer _callOffOrdersBusinessLayer;
        private readonly ContractBusinessLayer _contractBusinessLayer;
        private readonly TimeSheetsBusinessLayer _timeSheetsBusinessLayer;
        private readonly IMapper _autoMapper;

        public TimeSheetsService(IServiceProvider serviceProvider)
        {
            var _commandBuilder = (ICommandBuilder) serviceProvider.GetService(typeof(ICommandBuilder));
            var _queryBuilder = (IQueryBuilder) serviceProvider.GetService(typeof(IQueryBuilder));

            _autoMapper = (IMapper) serviceProvider.GetService(typeof(IMapper));

            _callOffOrdersBusinessLayer = new CallOffOrdersBusinessLayer(_commandBuilder, _queryBuilder);
            _contractBusinessLayer = new ContractBusinessLayer(_commandBuilder, _queryBuilder);
            _timeSheetsBusinessLayer = new TimeSheetsBusinessLayer(_commandBuilder, _queryBuilder);
        }

        #region GetDetailedTimeSheet

        public async Task<DetailedTimeSheetResponse> GetDetailedTimeSheetAsync(string timeSheetId)
        {
            var timeSheet = await _timeSheetsBusinessLayer.GetTimeSheet(timeSheetId);

            return await GetDetailedTimeSheetAsync(timeSheet);
        }

        private async Task<DetailedTimeSheetResponse> GetDetailedTimeSheetAsync(TimeSheet timeSheet)
        {
            var callOffOrder = await _callOffOrdersBusinessLayer.GetCallOffOrder(timeSheet.CallOffOrderId);

            return await GetDetailedTimeSheetAsync(timeSheet, callOffOrder);
        }

        private async Task<DetailedTimeSheetResponse> GetDetailedTimeSheetAsync(TimeSheet timeSheet,
            CallOffOrder callOffOrder)
        {
            var result = _autoMapper.Map<DetailedTimeSheetResponse>(timeSheet);

            result.WorkName = callOffOrder.Name;

            if (callOffOrder.TemplateSysName.ToLower() == "default")
            {
                result.AdditionalData = new DefaultAdditionalDataResponse();
            }
            else if (callOffOrder.TemplateSysName.ToLower() == "southtambey")
            {
                result.AdditionalData = new SouthTambeyAdditionalDataResponse();

                var additionalData = result.AdditionalData as SouthTambeyAdditionalDataResponse;
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
                if (!callOffOrderRate.IsRate) // группа
                {
                    var timeSheetRateGroup = new RateGroupResponse();
                    timeSheetRateGroup.Name = callOffOrderRate.Name;

                    var groupRates =
                        callOffOrder.Rates.Where(r => r.IsRate && r.ParentId == callOffOrderRate.Id).Select(r => r);


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

            DateTime startDate = DateTime.Parse(callOffOrder.StartDate).ToUniversalTime();
            DateTime finishDate = DateTime.Parse(callOffOrder.FinishDate).ToUniversalTime();

            var lastTimeSheet = (await _timeSheetsBusinessLayer.GetTimeSheetsByCallOffOrderId(callOffOrder.Id))
                .Where(t => t.Id != timeSheet.Id)
                .OrderBy(t => t.Year)
                .ThenBy(t => t.Month)
                .LastOrDefault();

            if (lastTimeSheet != null)
                startDate = new DateTime(lastTimeSheet.Year, lastTimeSheet.Month, 1).ToUniversalTime();


            if (startDate.AddMonths(1) > finishDate)
                startDate = startDate.AddMonths(1);
            

            result.AvailablePeriodsFrom = startDate;
            result.AvailablePeriodsTo = finishDate;


            return result;
        }

        #endregion

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

        public async Task UpdateSpentTimeAsync(string timeSheetId, string rateId, IEnumerable<double> spentTime)
        {
            var timeSheet = await _timeSheetsBusinessLayer.GetTimeSheet(timeSheetId);

            var callOffOrder = await _callOffOrdersBusinessLayer.GetCallOffOrder(timeSheet.CallOffOrderId);

            timeSheet.SpentTime[rateId] = spentTime;

            timeSheet.Amount = GetAmount(timeSheet, callOffOrder);

            await _timeSheetsBusinessLayer.UpdateTimeSheet(timeSheet);
        }

        public async Task UpdateAmountAsync(string timeSheetId)
        {
            var timeSheet = await _timeSheetsBusinessLayer.GetTimeSheet(timeSheetId);

            var callOffOrder = await _callOffOrdersBusinessLayer.GetCallOffOrder(timeSheet.CallOffOrderId);

            timeSheet.Amount = GetAmount(timeSheet, callOffOrder);

            await _timeSheetsBusinessLayer.UpdateTimeSheet(timeSheet);
        }

        public double GetAmount(TimeSheet timeSheet, CallOffOrder callOffOrder)
        {
            double result = 0;

            foreach (var timeSheetRateId in timeSheet.SpentTime.Keys)
            {
                var timeSheetSpantTime = timeSheet.SpentTime[timeSheetRateId];

                var callOffOrderRate = callOffOrder.Rates.Where(r => r.Id.ToString() == timeSheetRateId)
                    .SingleOrDefault();

                if (callOffOrderRate == null)
                    throw new ArgumentException("rate not found: " + timeSheetRateId);

                if (!callOffOrderRate.IsRate)
                    throw new ArgumentException(string.Format("rate {0} is group", timeSheetRateId));

                result += TimeSheetsBusinessLayer.GetAmount(callOffOrderRate.Amount,
                    ConvertToTimeUnit(callOffOrderRate.UnitName), timeSheetSpantTime);
            }

            return result;
        }

        public async Task UpdateTimeSheetAsync(string timeSheetId, string notes, int month, int year)
        {
            var timeSheet = await _timeSheetsBusinessLayer.GetTimeSheet(timeSheetId);

            timeSheet.Notes = notes;
            timeSheet.Month = month;
            timeSheet.Year = year;

            await _timeSheetsBusinessLayer.UpdateTimeSheet(timeSheet);
        }

        #region GetSimpleTimeSheets

        public async Task<IEnumerable<SimpleTimeSheetResponse>> GetSimpleTimeSheetsAsync()
        {
            var response = await _timeSheetsBusinessLayer.GetTimeSheets();

            return await GetSimpleTimeSheetsAsync(response);
        }

        private async Task<IEnumerable<SimpleTimeSheetResponse>> GetSimpleTimeSheetsAsync(
            IEnumerable<TimeSheet> timeSheets)
        {
            var result = new List<SimpleTimeSheetResponse>();

            foreach (var timeSheet in timeSheets)
            {
                var callOffOrder = await _callOffOrdersBusinessLayer.GetCallOffOrder(timeSheet.CallOffOrderId);
                var simpleTimeSheet = _autoMapper.Map<SimpleTimeSheetResponse>(timeSheet);

                simpleTimeSheet.Assignee = callOffOrder.Assignee;
                simpleTimeSheet.Position = callOffOrder.Position;
                simpleTimeSheet.WorkName = callOffOrder.Name;
                simpleTimeSheet.CallOffOrderId = callOffOrder.Id;

                result.Add(simpleTimeSheet);
            }

            return result;
        }

        #endregion

        public async Task<IEnumerable<SimpleTimeSheetResponse>> GetTimeSheetsByCallOffOrderAsync(string callOffOrderId)
        {
            var response =
                await _timeSheetsBusinessLayer.GetTimeSheetsByCallOffOrderId(callOffOrderId);

            return await GetSimpleTimeSheetsAsync(response);
        }

        /// <summary>
        /// Валидация табеля 
        /// </summary>
        /// <param name="timeSheet">Табель</param>
        /// <param name="callOffOrder">Наряд заказ, по кторому строится табель</param>
        /// <returns>Список предупреждений или пустой массив</returns>
        private IEnumerable<string> Validate(TimeSheet timeSheet, CallOffOrder callOffOrder)
        {
            var result = new List<string>();


            var containsFilledRate = false;

            foreach (var kvp in timeSheet.SpentTime)
            {
                var rateId = kvp.Key;
                var rateSpentTime = kvp.Value;

                var sum = kvp.Value.Sum(r => r);

                if (sum > 0)
                    containsFilledRate = true;

                var callOffRate = callOffOrder.Rates.Where(r => r.Id.ToString() == rateId).SingleOrDefault();

                if (callOffRate == null)
                    throw new Exception(string.Format("Ставка с id {0} не найдена в наряд заказе с id {1}", rateId,
                        callOffOrder.Id));


                var timeUnit = ConvertToTimeUnit(callOffRate.UnitName);

                switch (timeUnit)
                {
                    case TimeUnit.Day:
                        if (kvp.Value.Where(v => v > 1).Any())
                            result.Add(string.Format("'{0}' содержит значения больше 1", callOffRate.Name));
                        break;
                    case TimeUnit.Hour:
                        if (kvp.Value.Where(v => v > 24).Any())
                            result.Add(string.Format("'{0}' содержит значения больше 24", callOffRate.Name));
                        break;
                    default:
                        throw new Exception(string.Format("Неизвестный тип ставки callOffRate.UnitName  {0}",
                            callOffRate.UnitName));
                }
            }

            if (!containsFilledRate)
                result.Add("Табель не заполнен");

            if (timeSheet.Amount <= 0)
            {
                result.Add("У табеля нулевая сумма");
            }

            return result;
        }
    }
}