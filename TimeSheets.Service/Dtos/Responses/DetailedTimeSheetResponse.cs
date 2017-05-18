using System;
using System.Collections.Generic;
using Cmas.Services.TimeSheets.Dtos.Responses.Rate;
using Cmas.Services.TimeSheets.Dtos.Responses.AdditionalData;
using Newtonsoft.Json;
using Cmas.Services.TimeSheets.Converters;

namespace Cmas.Services.TimeSheets.Dtos.Responses
{
    /// <summary>
    /// Табель учета рабочего времени. Расширенный 
    /// Используется для просмотра/редактирования табелей
    /// </summary>
    public class DetailedTimeSheetResponse
    {
        /// <summary>
        /// Идентификатор табеля
        /// </summary>
        public string Id;

        /// <summary>
        /// Идентификатор наряд заказа
        /// </summary>
        public string CallOffOrderId;

        /// <summary>
        /// Месяц
        /// </summary>
        public int? Month;

        /// <summary>
        /// Год
        /// </summary>
        public int? Year;

        /// <summary>
        /// Дата и время создания
        /// </summary>
        public DateTime CreatedAt;

        /// <summary>
        /// Дата и время обновления
        /// </summary>
        public DateTime UpdatedAt;

        /// <summary>
        /// Наименование заказа (по сути - работы)
        /// </summary>
        public string WorkName;

        /// <summary>
        /// Сумма
        /// </summary>
        public double Amount;

        /// <summary>
        /// Валюта
        /// </summary>
        public string CurrencySysName;

        /// <summary>
        ///  Системное имя статуса
        /// </summary> 
        public string StatusSysName;

        /// <summary>
        ///  Наименованеи статуса
        /// </summary> 
        public string StatusName;


        /// <summary>
        /// Ставки/группы ставок и потраченное время
        /// </summary>
        public IList<RateGroupResponse> RateGroups;

        /// <summary>
        /// Доп. данные (в зависимости от шаблона)
        /// </summary>
        [JsonConverter(typeof(BaseAdditionalDataConverter))]
        public BaseAdditionalDataResponse AdditionalData;

        /// <summary>
        /// Примечания
        /// </summary>
        public string Notes;
         
        public DateTime? AvailablePeriodsFrom;

        public DateTime? AvailablePeriodsTo;

        public DetailedTimeSheetResponse()
        {
            RateGroups = new List<RateGroupResponse>(); 
        }

    }
}