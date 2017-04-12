namespace Cmas.Services.TimeSheets.Dtos.Responses.AdditionalData
{
    public class SouthTambeyAdditionalDataResponse : BaseAdditionalDataResponse
    {
        /// <summary>
        /// Тип данных
        /// </summary>
        public override string Type
        {
            get { return "southtambey"; }
        }

        /// <summary>
        /// Табельный номер
        /// </summary>
        public string EmployeeNumber;

        /// <summary>
        /// Номер позиции
        /// </summary>
        public string PositionNumber;

        /// <summary>
        /// Происхождение персонала
        /// </summary>
        public string PersonnelSource;

        /// <summary>
        /// Номер PAAF
        /// </summary>
        public string Paaf;

        /// <summary>
        /// Ссылка плана мобилизации
        /// </summary>
        public string MobPlanReference;

        /// <summary>
        /// Дата мобилизации
        /// </summary>
        public string MobDate;
    }
}
