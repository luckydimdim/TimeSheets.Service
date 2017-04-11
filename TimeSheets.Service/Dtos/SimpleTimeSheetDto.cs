﻿using System;

namespace Cmas.Services.TimeSheets.Dtos
{
    /// <summary>
    /// Табель учета рабочего времени. Упрощенный 
    /// Используется для вывода табелей в списках
    /// </summary>
    public class SimpleTimeSheetDto
    {
        /// <summary>
        /// Идентификатор табеля
        /// </summary>
        public string Id;

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
    }
}