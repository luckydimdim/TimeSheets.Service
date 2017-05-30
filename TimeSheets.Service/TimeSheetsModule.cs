using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cmas.Services.TimeSheets.Dtos.Requests;
using Cmas.Services.TimeSheets.Dtos.Responses;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Validation;
using System.Threading;
using Nancy.Responses.Negotiation;
using Cmas.Infrastructure.ErrorHandler;
using Cmas.Infrastructure.Security;

namespace Cmas.Services.TimeSheets
{
    public class RequestsModule : NancyModule
    {
        private IServiceProvider _serviceProvider;

        private TimeSheetsService timeSheetsService;

        private TimeSheetsService _timeSheetsService
        {
            get
            {
                if (timeSheetsService == null)
                    timeSheetsService = new TimeSheetsService(_serviceProvider, Context);

                return timeSheetsService;
            }
        }

        public RequestsModule(IServiceProvider serviceProvider) : base("/time-sheets")
        {
            this.RequiresRoles(new[] { Role.Contractor, Role.Customer });
            _serviceProvider = serviceProvider;

            

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
            /// Обновить статус табеля
            /// </summary>
            Put<Negotiator>("/{id}/status", UpdateStatusHandlerAsync);

            /// <summary>
            /// Пересчитать сумму по табелю
            /// </summary>
            Post<Negotiator>("/{id}/amount", UpdateAmountHandlerAsync);
             
            /// <summary>
            /// Обновить табель
            /// </summary>
            Put<Negotiator>("/{id}", UpdateTimeSheetHandlerAsync);
        }

        #region Обработчики

        private async Task<DetailedTimeSheetResponse> GetDetailedTimeSheetHandlerAsync(dynamic args,
            CancellationToken ct)
        {
            return await _timeSheetsService.GetDetailedTimeSheetAsync((string)args.id);
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
            return await _timeSheetsService.GetSimpleTimeSheetsAsync();
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

            await _timeSheetsService.UpdateTimeSheetAsync(args.id, request.Notes, request.From, request.Till);

            return Negotiate.WithStatusCode(HttpStatusCode.OK);
        }

        private async Task<Negotiator> UpdateAmountHandlerAsync(dynamic args, CancellationToken ct)
        {
          
            await _timeSheetsService.UpdateAmountAsync(args.id);

            return Negotiate.WithStatusCode(HttpStatusCode.OK);
        }

        private async Task<Negotiator> UpdateStatusHandlerAsync(dynamic args, CancellationToken ct)
        {
            UpdateStatusRequest request = this.Bind<UpdateStatusRequest>(new BindingConfig { BodyOnly = true });

            var validationResult = this.Validate(request);

            if (!validationResult.IsValid)
            {
                throw new ValidationErrorException(validationResult.FormattedErrors);
            }

            await _timeSheetsService.UpdateStatusAsync(args.id, request.Status);

            return Negotiate.WithStatusCode(HttpStatusCode.OK);
        }

        

        #endregion
    }
}