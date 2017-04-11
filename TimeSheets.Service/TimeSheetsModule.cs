using System;
using System.Collections.Generic;
using Nancy.Extensions;
using Cmas.BusinessLayers.CallOffOrders;
using Cmas.Infrastructure.Domain.Commands;
using Cmas.Infrastructure.Domain.Queries;
using Nancy;
using Nancy.ModelBinding;
using AutoMapper;
using System.Threading.Tasks;
using Cmas.BusinessLayers.Contracts;
using Nancy.IO;
using Cmas.Services.TimeSheets.Dtos;
using Cmas.BusinessLayers.TimeSheets;
using Cmas.BusinessLayers.TimeSheets.Entities;
using Cmas.BusinessLayers.CallOffOrders.Entities;

namespace Cmas.Services.Requests
{
    public class RequestsModule : NancyModule
    {
        private readonly ICommandBuilder _commandBuilder;
        private readonly IQueryBuilder _queryBuilder;
        private readonly CallOffOrdersBusinessLayer _callOffOrdersBusinessLayer;
        private readonly ContractBusinessLayer _contractBusinessLayer;
        private readonly TimeSheetsBusinessLayer _timeSheetsBusinessLayer;
        private readonly IMapper _autoMapper;

        private async Task<DetailedTimeSheetDto> GetDetailedTimeSheet(string timeSheetId)
        {
            var timeSheet = await _timeSheetsBusinessLayer.GetTimeSheet(timeSheetId);

            return await GetDetailedTimeSheet(timeSheet);
        }

        private async Task<DetailedTimeSheetDto> GetDetailedTimeSheet(TimeSheet timeSheet)
        {
            DetailedTimeSheetDto result = _autoMapper.Map<DetailedTimeSheetDto>(timeSheet);

            CallOffOrder callOffOrder = await _callOffOrdersBusinessLayer.GetCallOffOrder(timeSheet.CallOffOrderId);

            result.WorkName = callOffOrder.Name;
            result.Assignee = callOffOrder.Assignee;
            result.Position = callOffOrder.Position;

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
            /// /time-sheets/{id} - получить табель по указанному ID
            /// </summary>
            /// <return>DetailedTimeSheetDto</return>
            Get("/{id}", async args =>
            {
                return await GetDetailedTimeSheet(args.id);
            });

            /// <summary>
            /// Создать табель
            /// </summary>
            /// <return>DetailedTimeSheetDto</return>
            Post("/", async (args, ct) =>
            {
                var createTimeSheetDto = this.Bind<CreateTimeSheetDto>();

                string requestId = await _timeSheetsBusinessLayer.CreateTimeSheet(createTimeSheetDto.CallOffOrderId);

                return await GetDetailedTimeSheet(requestId);
            });

        }
    }
}