namespace HermesWebApi.Models
{
    public struct ResultCode
    {
        public int ErrorNo { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorMessageEn { get; set; }

        public static bool operator ==(ResultCode res1, ResultCode res2)
        {
            return res1.ErrorNo == res2.ErrorNo;
        }
        public static bool operator !=(ResultCode res1, ResultCode res2)
        {
            return res1.ErrorNo != res2.ErrorNo;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is ResultCode))
                return false;
            return ((ResultCode)obj).ErrorNo == ErrorNo;
        }
    }
    public class ResultCodes
    {
        public static ResultCode noError = new ResultCode() { ErrorNo = 0, ErrorMessage = "Əməliyyat müvəffəqiyyətlə tamamlandı", ErrorMessageEn = "Successfully Saved" };
        public static ResultCode dbError = new ResultCode() { ErrorNo = 1, ErrorMessage = "Databaza xətası", ErrorMessageEn = "Database error!" };
        public static ResultCode IDNotFoundError = new ResultCode() { ErrorNo = 1, ErrorMessage = "Aradığınız kayıt veri tabanında bulunamadı!", ErrorMessageEn = "Record not found in database!" };
        public static ResultCode codeAlreadyExistsError = new ResultCode() { ErrorNo = 2, ErrorMessage = "Bu kodlu kayıt sistemde var!", ErrorMessageEn = "This code already exists!" };
        public static ResultCode nameAlreadyExistsError = new ResultCode() { ErrorNo = 3, ErrorMessage = "Bu isimli kayıt sistemde var!", ErrorMessageEn = "This name already exists!" };
        public static ResultCode nullIDError = new ResultCode() { ErrorNo = 4, ErrorMessage = "Kaydetmediğiniz kaydı güncelleyemezsiniz!", ErrorMessageEn = "You can not update item which you have not saved yet!" };
        public static ResultCode zeroAmountRequestERROR = new ResultCode() { ErrorNo = 5, ErrorMessage = "Değeri sıfır(0) olan talebe işlem yapamazsınız!", ErrorMessageEn = "You can not process request with zero amount!" };
        public static ResultCode zeroAmountBidERROR = new ResultCode() { ErrorNo = 6, ErrorMessage = "Değeri sıfır(0) olan teklife işlem yapamazsınız!", ErrorMessageEn = "You can not process bid with zero amount!" };
        public static ResultCode zeroMoveRequestERROR = new ResultCode() { ErrorNo = 7, ErrorMessage = "Mal kabülü olmayan talebi kapatamazsınız!", ErrorMessageEn = "You can not process request which has not inventory in!" };
        public static ResultCode incorrectDateInterval = new ResultCode() { ErrorNo = 8, ErrorMessage = "Hatalı tarih aralığı. Daha önceden bu aralığa kayıt girilmiş!", ErrorMessageEn = "Incorrect Date intervar!" };
        public static ResultCode sessionTimeoutError = new ResultCode() { ErrorNo = 9, ErrorMessage = "Oturumunuz zaman asımına uğradı. Lütfen yeniden oturum açınız!", ErrorMessageEn = "Session timout. Please login again!" };
        public static ResultCode noBidERROR = new ResultCode() { ErrorNo = 6, ErrorMessage = "En az bir önerilen teklifi olmayan talebe bu işlemi yapamazsınız!", ErrorMessageEn = "There must be at least one recommended bid for the request!" };

    }
}
