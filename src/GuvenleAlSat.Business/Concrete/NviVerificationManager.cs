using System.Globalization;
using System.Text;
using System.Xml.Linq;
using GuvenleAlSat.Business.Abstract;
using GuvenleAlSat.Core.Utilities.Results;

namespace GuvenleAlSat.Business.Concrete;

public class NviVerificationManager : INviVerificationService
{
    private readonly HttpClient _httpClient;

    public NviVerificationManager(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IDataResult<bool>> VerifyAsync(string nationalId, string firstName, string lastName, int birthYear)
    {
        // 1. Temel Format Kontrolü
        if (string.IsNullOrWhiteSpace(nationalId) || nationalId.Trim().Length != 11 || !long.TryParse(nationalId.Trim(), out long tcNo))
            return new ErrorDataResult<bool>(false, "Geçersiz T.C. Kimlik Numarası formatı.");

        string tcStr = nationalId.Trim();
        if (tcStr.StartsWith("0"))
            return new ErrorDataResult<bool>(false, "T.C. Kimlik Numarası 0 ile başlayamaz.");

        // 2. Resmi T.C. Kimlik Doğrulama Algoritması
        int[] d = tcStr.Select(c => c - '0').ToArray();
        int oddSum = d[0] + d[2] + d[4] + d[6] + d[8];
        int evenSum = d[1] + d[3] + d[5] + d[7];

        int tenthDigit = ((oddSum * 7) - evenSum) % 10;
        if (tenthDigit < 0) tenthDigit += 10;
        if (d[9] != tenthDigit)
            return new ErrorDataResult<bool>(false, "T.C. Kimlik Numarası algoritma doğrulamasından geçemedi.");

        int eleventhDigit = d.Take(10).Sum() % 10;
        if (d[10] != eleventhDigit)
            return new ErrorDataResult<bool>(false, "T.C. Kimlik Numarası algoritma doğrulamasından geçemedi.");

        if (birthYear < 1920 || birthYear > DateTime.Now.Year - 18)
            return new ErrorDataResult<bool>(false, "Geçersiz doğum yılı veya 18 yaşından küçük kullanıcı.");

        // 3. Türkçe İsim Büyütme
        var culture = new CultureInfo("tr-TR");
        var upperFirstName = firstName.Trim().ToUpper(culture);
        var upperLastName = lastName.Trim().ToUpper(culture);

        var soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <TCKimlikNoDogrula xmlns=""http://tckimlik.nvi.gov.tr/WS"">
      <TCKimlikNo>{tcNo}</TCKimlikNo>
      <Ad>{upperFirstName}</Ad>
      <Soyad>{upperLastName}</Soyad>
      <DogumYili>{birthYear}</DogumYili>
    </TCKimlikNoDogrula>
  </soap:Body>
</soap:Envelope>";

        try
        {
            // Doğru NVİ Endpoint Adresi: /Service/KPSPublic.asmx
            var request = new HttpRequestMessage(HttpMethod.Post, "https://tckimlik.nvi.gov.tr/Service/KPSPublic.asmx")
            {
                Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml")
            };
            request.Headers.Add("SOAPAction", "http://tckimlik.nvi.gov.tr/WS/TCKimlikNoDogrula");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<bool>(false, "Nüfus Müdürlüğü doğrulama servisine bağlanılamadı.");

            var xmlResponse = await response.Content.ReadAsStringAsync();

            XNamespace ns = "http://tckimlik.nvi.gov.tr/WS";
            var doc = XDocument.Parse(xmlResponse);
            var resultNode = doc.Descendants(ns + "TCKimlikNoDogrulaResult").FirstOrDefault();

            bool isSuccess = resultNode != null && bool.TryParse(resultNode.Value, out var res) && res;

            if (isSuccess)
                return new SuccessDataResult<bool>(true, "Kimlik bilgileri Nüfus Müdürlüğü tarafından doğrulandı.");

            return new ErrorDataResult<bool>(false, "Girilen T.C. Kimlik No, Ad, Soyad veya Doğum Yılı Nüfus Müdürlüğü kayıtlarıyla uyuşmuyor.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NVİ İSTİSNA]: {ex.Message}");
            return new ErrorDataResult<bool>(false, "Nüfus Müdürlüğü servisinde geçici bir kesinti var. Lütfen bilgilerinizi kontrol edip tekrar deneyin.");
        }
    }
}