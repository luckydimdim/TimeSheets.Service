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
using System.Threading;
using Nancy.Responses.Negotiation;
using Cmas.Infrastructure.ErrorHandler;

namespace Cmas.Services.TimeSheets
{
    public class RequestsModule : NancyModule
    {
        private readonly TimeSheetsService _timeSheetsService;

        public RequestsModule(IServiceProvider serviceProvider) : base("/time-sheets")
        {
            _timeSheetsService = new TimeSheetsService(serviceProvider);

            /// <summary>
            /// /time-sheets/ - получить список табелей
            /// </summary>
            Get<IEnumerable<SimpleTimeSheetResponse>>("/", GetSimpleTimeSheetsHandlerAsync,
                (ctx) => !ctx.Request.Query.ContainsKey("callOffOrderId"));

            /// <summary>
            /// /time-sheets?callOffOrderId={id} - получить табели по указанному договору
            /// </summary>
            Get<IEnumerable<SimpleTimeSheetResponse>>("/",
                GetSimpleTimeSheetsByCallOffOrderHandlerAsync,
                (ctx) => ctx.Request.Query.ContainsKey("callOffOrderId"));

            /// <summary>
            /// /time-sheets/{id} - получить табель по указанному ID
            /// </summary>
            Get<DetailedTimeSheetResponse>("/{id}", GetDetailedTimeSheetHandlerAsync);

            /// <summary>
            /// Создать табель учета рабочего времени
            /// </summary>
            Post<DetailedTimeSheetResponse>("/", CreateTimeSheetHandlerAsync);

            /// <summary>
            /// Обновить отработанное время по работам
            /// </summary>
            Put<Negotiator>("/{id}/spent-time", UpdateSpentTimeHandlerAsync);

            /// <summary>
            /// Пересчитать сумму по табелю
            /// </summary>
            Post<Negotiator>("/{id}/amount", UpdateAmountHandlerAsync);
             
            /// <summary>
            /// Обновить табель
            /// </summary>
            Put<Negotiator>("/{id}", UpdateTimeSheetHandlerAsync);
        }

        #region Обрабочтики

        private async Task<DetailedTimeSheetResponse> GetDetailedTimeSheetHandlerAsync(dynamic args,
            CancellationToken ct)
        {
            return await _timeSheetsService.GetDetailedTimeSheetAsync(args.id);
        }

        private async Task<IEnumerable<SimpleTimeSheetResponse>> GetSimpleTimeSheetsByCallOffOrderHandlerAsync(
            dynamic args,
            CancellationToken ct)
        {
            return await _timeSheetsService.GetTimeSheetsByCallOffOrderAsync(Request.Query["callOffOrderId"]);
        }

        private async Task<IEnumerable<SimpleTimeSheetResponse>> GetSimpleTimeSheetsHandlerAsync(dynamic args,
            CancellationToken ct)
        {
            return await _timeSheetsService.GetTimeSheetsAsync();
        }

        private Task<DetailedTimeSheetResponse> CreateTimeSheetHandlerAsync(dynamic args, CancellationToken ct)
        {
            throw new NotImplementedException(); // Создание TS возможно только из сервиса Заявок на проверку
        }

        private async Task<Negotiator> UpdateSpentTimeHandlerAsync(dynamic args, CancellationToken ct)
        {
            UpdateSpentTimesRequest request = this.Bind<UpdateSpentTimesRequest>(new BindingConfig {BodyOnly = true});

            var validationResult = this.Validate(request);

            if (!validationResult.IsValid)
            {
                throw new ValidationErrorException(validationResult.FormattedErrors);
            }

            await _timeSheetsService.UpdateSpentTimeAsync(args.id, request.Id, request.SpentTime);

            return Negotiate.WithStatusCode(HttpStatusCode.OK);
        }

        private async Task<Negotiator> UpdateTimeSheetHandlerAsync(dynamic args, CancellationToken ct)
        {
            UpdateTimeSheetRequest request = this.Bind<UpdateTimeSheetRequest>(new BindingConfig { BodyOnly = true });

            var validationResult = this.Validate(request);

            if (!validationResult.IsValid)
            {
                throw new ValidationErrorException(validationResult.FormattedErrors);
            }

            await _timeSheetsService.UpdateTimeSheetAsync(args.id, request.Notes, request.Month, request.Year);

            return Negotiate.WithStatusCode(HttpStatusCode.OK);
        }

        private async Task<Negotiator> UpdateAmountHandlerAsync(dynamic args, CancellationToken ct)
        {
          
            await _timeSheetsService.UpdateAmountAsync(args.id);

            return Negotiate.WithStatusCode(HttpStatusCode.OK);
        }

        #endregion
    }
}