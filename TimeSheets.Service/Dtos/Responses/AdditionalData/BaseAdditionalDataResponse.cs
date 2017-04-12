namespace Cmas.Services.TimeSheets.Dtos.Responses.AdditionalData
{
    /// <summary>
    /// Базовый тип для доп. данных
    /// </summary>
    public class BaseAdditionalDataResponse
    {
        /// <summary>
        /// Тип данных
        /// </summary>
        public virtual string Type
        {
            get { return "unknown"; }
        }

        /// <summary>
        /// ФИО работника
        /// </summary>
        public string Assignee = "";

        /// <summary>
        /// Должность
        /// </summary>
        public string Position;

        /// <summary>
        /// Место работы
        /// </summary>
        public string Location;
    }
}
