using AutoMapper;
using Cmas.BusinessLayers.CallOffOrders;
using Cmas.BusinessLayers.Contracts;
using Cmas.BusinessLayers.TimeSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cmas.BusinessLayers.CallOffOrders.Entities;
using Cmas.BusinessLayers.TimeSheets.Entities;
using Cmas.Services.TimeSheets.Dtos.Responses;
using Cmas.Services.TimeSheets.Dtos.Responses.AdditionalData;
using Cmas.Services.TimeSheets.Dtos.Responses.Rate;
using Nancy;
using Cmas.BusinessLayers.Requests;
using Cmas.BusinessLayers.Requests.Entities;
using Cmas.Infrastructure.ErrorHandler;
using System.IO;

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
            result.UnitName = callOffOrderRate.UnitName;

            if (spentTime != null && spentTime.ContainsKey(rateId))
                result.SpentTime = spentTime[rateId].ToList();

            return result;
        }
    }

    public class TimeSheetsService
    {
        private readonly CallOffOrdersBusinessLayer _callOffOrdersBusinessLayer;
        private readonly ContractsBusinessLayer _contractsBusinessLayer;
        private readonly TimeSheetsBusinessLayer _timeSheetsBusinessLayer;
        private readonly RequestsBusinessLayer _requestsBusinessLayer;
        private readonly IMapper _autoMapper;

        public TimeSheetsService(IServiceProvider serviceProvider, NancyContext ctx)
        {
            _autoMapper = (IMapper) serviceProvider.GetService(typeof(IMapper));

            _callOffOrdersBusinessLayer = new CallOffOrdersBusinessLayer(serviceProvider, ctx.CurrentUser);
            _contractsBusinessLayer = new ContractsBusinessLayer(serviceProvider, ctx.CurrentUser);
            _timeSheetsBusinessLayer = new TimeSheetsBusinessLayer(serviceProvider, ctx.CurrentUser);
            _requestsBusinessLayer = new RequestsBusinessLayer(serviceProvider, ctx.CurrentUser);
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
                else if (callOffOrderRate.IsRate && string.IsNullOrEmpty(callOffOrderRate.ParentId))
                {
                    var timeSheetRateGroup = new RateGroupResponse();

                    var timeSheetRate = callOffOrderRate.ConvertToTimeSheetRate(timeSheet.SpentTime);
                    timeSheetRateGroup.Rates.Add(timeSheetRate);

                    result.RateGroups.Add(timeSheetRateGroup);
                }

            IEnumerable<TimeSheet> callOffTimeSheets =
                await _timeSheetsBusinessLayer.GetTimeSheetsByCallOffOrderId(callOffOrder.Id);

            result.StatusSysName = timeSheet.Status.ToString();
            result.StatusName = TimeSheetsBusinessLayer.GetStatusName(timeSheet.Status);

            result.CallOffOrderStartDate = callOffOrder.StartDate;
            result.CallOffOrderFinishDate = callOffOrder.FinishDate;


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

            if (timeSheet == null)
            {
                throw new NotFoundErrorException();
            }

            var callOffOrder = await _callOffOrdersBusinessLayer.GetCallOffOrder(timeSheet.CallOffOrderId);

            timeSheet.SpentTime[rateId] = spentTime;

            timeSheet.Amount = GetAmount(timeSheet, callOffOrder);

            await _timeSheetsBusinessLayer.UpdateTimeSheet(timeSheet);
        }

        public async Task UpdateStatusAsync(string timeSheetId, TimeSheetStatus status)
        {
            if (status == TimeSheetStatus.None)
                throw new ArgumentException("status");

            var timeSheet = await _timeSheetsBusinessLayer.GetTimeSheet(timeSheetId);

            if (timeSheet == null)
            {
                throw new NotFoundErrorException();
            }

            await _timeSheetsBusinessLayer.UpdateTimeSheetStatus(timeSheet, status);

            // TODO: переделать под событийную модель (с шиной)
            await UpdateRequestStatusAsync(timeSheet.RequestId);
        }

        private async Task UpdateRequestStatusAsync(string requestId)
        {
            var timeSheets = await _timeSheetsBusinessLayer.GetTimeSheetsByRequestId(requestId);
            var request = await _requestsBusinessLayer.GetRequest(requestId);

            if (timeSheets.Count() == timeSheets.Where(t => t.Status == TimeSheetStatus.Created).Count())
            {
                await _requestsBusinessLayer.UpdateRequestStatusAsync(request, RequestStatus.Created);
            }
            else if (timeSheets.Where(t => t.Status == TimeSheetStatus.Creating || t.Status == TimeSheetStatus.Created)
                .Any())
            {
                await _requestsBusinessLayer.UpdateRequestStatusAsync(request, RequestStatus.Creating);
            }
            else if (timeSheets.Count() == timeSheets
                         .Where(t => t.Status == TimeSheetStatus.Corrected || t.Status == TimeSheetStatus.Approved)
                         .Count() && timeSheets.Any(t => t.Status == TimeSheetStatus.Corrected))
            {
                await _requestsBusinessLayer.UpdateRequestStatusAsync(request, RequestStatus.Corrected);
            }
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

        /// <summary>
        /// Обновить табель
        /// </summary>
        /// <param name="timeSheetId">ID табеля</param>
        /// <param name="notes">Примечания</param>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public async Task UpdateTimeSheetAsync(string timeSheetId, string notes, DateTime from, DateTime till)
        {
            TimeSheet timeSheet = await _timeSheetsBusinessLayer.GetTimeSheet(timeSheetId);

            if (timeSheet == null)
            {
                throw new NotFoundErrorException();
            }

            timeSheet.Notes = notes;

            // сбрасываем работы
            if (timeSheet.From != from || timeSheet.Till != till)
            {
                timeSheet.SpentTime = new Dictionary<string, IEnumerable<double>>();
                timeSheet.Amount = 0;
            }

            timeSheet.From = from;
            timeSheet.Till = till;

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
                simpleTimeSheet.StatusSysName = timeSheet.Status.ToString();
                simpleTimeSheet.StatusName = TimeSheetsBusinessLayer.GetStatusName(timeSheet.Status);

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
                            result.Add($"'{callOffRate.Name}' содержит значения больше 1");
                        break;
                    case TimeUnit.Hour:
                        if (kvp.Value.Where(v => v > 24).Any())
                            result.Add($"'{callOffRate.Name}' содержит значения больше 24");
                        break;
                    default:
                        throw new Exception($"Неизвестный тип ставки callOffRate.UnitName  {callOffRate.UnitName}");
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

        /// <summary>
        /// Добавить вложение
        /// </summary>
        public async Task<FileUploadResponse> AddAttachmentAsync(string timeSheetId, string fileName, Stream stream,
            string contentType)
        {
            var timeSheet = await _timeSheetsBusinessLayer.GetTimeSheet(timeSheetId);

            if (timeSheet == null)
            {
                throw new NotFoundErrorException();
            }

            if (timeSheet.Attachments.ContainsKey(fileName))
            {
                throw new InvalidOperationException("attachment with this name already exists");
            }

            var result = new FileUploadResponse();

            result.Identifier = await _timeSheetsBusinessLayer.AddAttachment(timeSheet, fileName, stream, contentType);

            return result;
        }

        /// <summary>
        /// Удалить вложение
        /// </summary>
        public async Task DeleteAttachmentAsync(string timeSheetId, string fileName)
        {
            var timeSheet = await _timeSheetsBusinessLayer.GetTimeSheet(timeSheetId);

            if (timeSheet == null)
            {
                throw new NotFoundErrorException();
            }

            if (!timeSheet.Attachments.ContainsKey(fileName))
            {
                throw new InvalidOperationException("attachment with this name not exists");
            }

            await _timeSheetsBusinessLayer.DeleteAttachmentAsync(timeSheet, fileName);
        }

        /// <summary>
        /// Получить вложение
        /// </summary>
        public async Task<Attachment> GetAttachmentAsync(string timeSheetId, string fileName)
        {
            var timeSheet = await _timeSheetsBusinessLayer.GetTimeSheet(timeSheetId);

            if (timeSheet == null)
            {
                throw new NotFoundErrorException();
            }

            if (!timeSheet.Attachments.ContainsKey(fileName))
            {
                throw new InvalidOperationException("attachment with this name not exists");
            }

            return await _timeSheetsBusinessLayer.GetAttachmentAsync(timeSheet, fileName);
        }

        /// <summary>
        /// Получить вложения (без данных)
        /// </summary>
        public async Task<AttachmentResponse[]> GetAttachmentsAsync(string timeSheetId)
        {
            var result = new List<AttachmentResponse>();

            var attachments = await _timeSheetsBusinessLayer.GetAttachmentsAsync(timeSheetId);

            foreach (var attachment in attachments)
            {
                result.Add(_autoMapper.Map<AttachmentResponse>(attachment));
            }

            return result.ToArray();
        }
    }
}