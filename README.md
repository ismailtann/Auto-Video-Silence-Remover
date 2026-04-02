# 🎬 Tan Tecno - AI Video & Audio Auto Cutter

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET 10.0](https://img.shields.io/badge/.NET_8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![FFmpeg](https://img.shields.io/badge/FFmpeg-007808?style=for-the-badge&logo=ffmpeg&logoColor=white)

**Tan Tecno** YouTube kanalımın içerik üretim süreçlerini otomatize etmek ve kurgu süresini minimize etmek için kendi geliştirdiğim, sese duyarlı otomatik video kesme (Auto-Cutter) aracı.

---

### 💡 Projenin Amacı
YouTube veya eğitim videoları çekerken oluşan nefes aralarını, duraksamaları ve sessizlikleri video edit programlarında (Premiere, Resolve vb.) manuel olarak kesmek saatler alır. Bu araç, belirlediğiniz desibel (-dB) eşiğinin altında kalan tüm sessiz anları yapay zeka ve matematiksel algoritmalar kullanarak otomatik tespit eder, keser ve videoyu senkronu bozmadan tek parça halinde geri birleştirir.

---

### 🧠 Mimari ve Algoritma (Split & Concat)
Bu yazılım basit bir komut çalıştırıcısı değildir. Arka planda şu mühendislik adımlarını izler:
1. **Analiz (Detection):** Asenkron işlem (Process) yönetimi ile FFmpeg üzerinden videonun ses dalgaları taranır ve `-38dB` altındaki sessizliklerin milisaniye cinsinden başlangıç/bitiş zamanları tespit edilir.
2. **Matematiksel Ayrıştırma (Parsing):** C# `Regex` kullanılarak konsola akan devasa veri yığını parçalanır ve sadece "konuşulan" kısımların matematiksel haritası (`List<Tuple>`) çıkarılır.
3. **Parçalama (Chunking):** Görüntü ve ses senkronunun kaymaması için video, tespit edilen konuşma sürelerine göre yüzlerce küçük parçaya ayrılır.
4. **Birleştirme (Concatenation):** Kesilen bu saf parçalar, aralarında boşluk kalmayacak şekilde render edilerek tek bir `.mp4` dosyası haline getirilir.

---

### 🚀 Özellikler
- **Tam Otomasyon:** `ham_videolar` klasörüne atılan tüm videoları sırayla işler (Batch Processing).
- **Hata Yönetimi:** Klasör yokluğu, yanlış dosya formatı gibi durumlarda `Try-Catch` bloklarıyla kullanıcıyı yönlendirir.
- **Kültür Bağımsız Çalışma:** Ondalık sayı (nokta/virgül) uyuşmazlıklarını önlemek için `CultureInfo.InvariantCulture` entegrasyonu.
- **Tek Parça Kurulumsuz Kullanım (Single File):** .NET kütüphaneleri içine gömülmüş bağımsız `.exe` mimarisi.

---

### 📥 Nasıl Kullanılır?
Geliştirici değilseniz veya kodu derlemekle uğraşmak istemiyorsanız:
1. Sağ taraftaki **[Releases]** sekmesinden güncel `.exe` dosyasını indirin.
2. Programın bulunduğu yere `ham_videolar` adında bir klasör açıp içine videolarınızı atın.
3. Programı çalıştırın ve `cikti` klasöründen hazır videolarınızı alın!
