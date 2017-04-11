using System;
using System.Collections.Generic;

namespace Cmas.Services.TimeSheets.Dtos
{
    /// <summary>
    /// Табель учета рабочего времени. Расширенный 
    /// Используется для просмотра/редактирования табелей
    /// </summary>
    public class DetailedTimeSheetDto
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
        /// ФИО
        /// </summary>
        public string Assignee;

        /// <summary>
        /// Должность
        /// </summary>
        public string Position;

        /// <summary>
        /// Сумма
        /// </summary>
        public double Amount;

        /// <summary>
        /// TODO: Валюта
        /// </summary>


        /// <summary>
        /// TODO: Статус
        /// </summary> 

        /// <summary>
        /// Рабочее время в разрезе работ
        /// Dictionary<{ID ставки}, IEnumerable<{время по каждому дню в месяце}>>
        /// </summary>
        public Dictionary<string, IEnumerable<double>> Times;

        /// <summary>
        /// Примечания
        /// </summary>
        public string Notes;

        public DetailedTimeSheetDto()
        {
            Times = new Dictionary<string, IEnumerable<double>>();
        }

    }
}