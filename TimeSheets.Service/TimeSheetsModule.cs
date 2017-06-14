using System;
using System.Collections.Generic;
using System.IO;
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
using Nancy.Responses;
using Cmas.BusinessLayers.TimeSheets.Entities;

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
            this.RequiresAnyRole(new[] {Role.Contractor, Role.Customer}, except: new[] { "download-attachment" });
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

            /// <summary>
            /// Загрузить вложение
            /// </summary>
            Post<FileUploadResponse>("/{id}/attachment", UploadAttachmentHandlerAsync);

            /// <summary>
            /// Удалить вложение
            /// </summary>
            Delete<Negotiator>("/{id}/attachment/{fileName}", DeleteAttachmentHandlerAsync);

            /// <summary>
            /// Получить вложение
            /// </summary>
            Get<Response>("/{id}/attachment/{fileName}", GetAttachmentHandlerAsync, name: "download-attachment");

            /// <summary>
            /// Сгенерировать временный токен для скачки файла
            /// </summary>
            Post<string>("/{id}/attachment-token/{fileName}", CreateTempAttachmentTokenHandlerAsync);

            /// <summary>
            /// Получить вложения (без данных)
            /// </summary>
            Get<AttachmentResponse[]>("/{id}/attachments", GetAttachmentsHandlerAsync);
        }

        #region Обработчики

        private async Task<string> CreateTempAttachmentTokenHandlerAsync(dynamic args,
            CancellationToken ct)
        {
            string fileName = Uri.EscapeDataString((string)args.fileName);
            return await _timeSheetsService.CreateTempAttachmentTokenAsync((string) args.id, fileName);
        }

        private async Task<AttachmentResponse[]> GetAttachmentsHandlerAsync(dynamic args,
            CancellationToken ct)
        {
            return await _timeSheetsService.GetAttachmentsAsync((string) args.id);
        }

        private async Task<Response> GetAttachmentHandlerAsync(dynamic args,
            CancellationToken ct)
        {
            string token = Request.Query["token"];
            string fileName = Uri.EscapeDataString((string)args.fileName);

            Attachment attachment = await _timeSheetsService.GetAttachmentAsync((string) args.id, fileName, token);

            var response = new StreamResponse(() => new MemoryStream(attachment.Data), attachment.Content_type);
            return response .AsAttachment(fileName, contentType: attachment.Content_type);
        }

        private async Task<Negotiator> DeleteAttachmentHandlerAsync(dynamic args,
            CancellationToken ct)
        {

            string fileName = Uri.EscapeDataString((string) args.fileName);
            await _timeSheetsService.DeleteAttachmentAsync((string) args.id, fileName);

            return Negotiate.WithStatusCode(HttpStatusCode.OK);
        }

        private async Task<FileUploadResponse> UploadAttachmentHandlerAsync(dynamic args,
            CancellationToken ct)
        {
            var request = this.Bind<FileUploadRequest>();

            return await _timeSheetsService.AddAttachmentAsync((string) args.id, request.File.Name, request.File.Value,
                request.File.ContentType);
        }

        private async Task<DetailedTimeSheetResponse> GetDetailedTimeSheetHandlerAsync(dynamic args,
            CancellationToken ct)
        {
            return await _timeSheetsService.GetDetailedTimeSheetAsync((string) args.id);
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
            UpdateTimeSheetRequest request = this.Bind<UpdateTimeSheetRequest>(new BindingConfig {BodyOnly = true});

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
            UpdateStatusRequest request = this.Bind<UpdateStatusRequest>(new BindingConfig {BodyOnly = true});

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