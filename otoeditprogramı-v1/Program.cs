using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Threading;

class Program
{
    // --- AYARLAR ---
    static string inputFolder = "ham_videolar";
    static string outputFolder = "cikti";
    static string tempFolder = "temp_parcalar";
    static string ffmpegPath = "ffmpeg";

    // Hassasiyet Ayarları (Kendi mikrofonuna göre burayı değiştirebilirsin)
    static string silenceDb = "-38dB"; // -38dB idealdir. Çok kesiyorsa -45dB yap, az kesiyorsa -30dB yap.
    static string silenceDuration = "0.5"; // 0.5 saniyeden uzun sessizlikleri keser

    static void Main()
    {
        Console.Title = "Tan Tecno - AI Video & Audio Auto Cutter";

        // TÜRKÇE BİLGİSAYARLARDA NOKTA/VİRGÜL HATASINI ÖNLEMEK İÇİN KRİTİK KOD!
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        // Klasörleri Hazırla
        Directory.CreateDirectory(inputFolder);
        Directory.CreateDirectory(outputFolder);
        if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        Directory.CreateDirectory(tempFolder);

        string[] videos = Directory.GetFiles(inputFolder, "*.mp4");

        if (videos.Length == 0)
        {
            Console.WriteLine($"[UYARI] '{inputFolder}' klasöründe .mp4 dosyası yok!");
            Console.ReadKey();
            return;
        }

        foreach (var videoFile in videos)
        {
            string fileName = Path.GetFileName(videoFile);
            string finalOutputFile = Path.Combine(outputFolder, "EDITLENMIS_" + fileName);

            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"🎬 İŞLEM BAŞLADI: {fileName}");
            Console.WriteLine(new string('=', 60));

            // AŞAMA 1: VİDEO UZUNLUĞUNU VE SESSİZLİKLERİ BUL
            Console.WriteLine("🔍 1. Aşama: Yapay Zeka videoyu dinliyor (Sessizlik Taraması)...");

            List<double> silenceStarts = new List<double>();
            List<double> silenceEnds = new List<double>();
            double totalDuration = 0;

            string detectArgs = $"-i \"{videoFile}\" -af \"silencedetect=noise={silenceDb}:d={silenceDuration}\" -f null -";

            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = detectArgs,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;

                    // Toplam Süreyi Yakala
                    Match durMatch = Regex.Match(e.Data, @"Duration: (\d+):(\d+):([\d\.]+)");
                    if (durMatch.Success && totalDuration == 0)
                    {
                        TimeSpan ts = new TimeSpan(0, int.Parse(durMatch.Groups[1].Value), int.Parse(durMatch.Groups[2].Value), 0, (int)(double.Parse(durMatch.Groups[3].Value) * 1000));
                        totalDuration = ts.TotalSeconds;
                    }

                    // Sessizlik Başlangıçlarını Yakala
                    Match startMatch = Regex.Match(e.Data, @"silence_start:\s+([\d\.]+)");
                    if (startMatch.Success) silenceStarts.Add(double.Parse(startMatch.Groups[1].Value));

                    // Sessizlik Bitişlerini (Konuşma Başlangıçlarını) Yakala
                    Match endMatch = Regex.Match(e.Data, @"silence_end:\s+([\d\.]+)");
                    if (endMatch.Success) silenceEnds.Add(double.Parse(endMatch.Groups[1].Value));
                };

                process.Start();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }

            // AŞAMA 2: KESİLECEK (TUTULACAK) PARÇALARI HESAPLA
            Console.WriteLine("🧠 2. Aşama: Matematiksel Kesim Noktaları Hesaplanıyor...");
            List<Tuple<double, double>> keepSegments = new List<Tuple<double, double>>();
            double currentPos = 0;

            for (int i = 0; i < silenceStarts.Count; i++)
            {
                if (silenceStarts[i] > currentPos + 0.2) // En az 0.2 saniyelik konuşma varsa tut
                {
                    keepSegments.Add(new Tuple<double, double>(currentPos, silenceStarts[i]));
                }
                if (i < silenceEnds.Count) currentPos = silenceEnds[i];
            }

            // Son sessizlikten videonun sonuna kadar olan kısmı ekle
            if (currentPos < totalDuration - 0.2)
            {
                keepSegments.Add(new Tuple<double, double>(currentPos, totalDuration));
            }

            Console.WriteLine($"✂️ Toplam {keepSegments.Count} adet konuşma parçası çıkarılacak.");

            // AŞAMA 3: PARÇALARI AYRI AYRI KES (RENDER)
            Console.WriteLine("🔪 3. Aşama: Parçalar Jilet Gibi Kesiliyor (Bu işlem bilgisayarın hızına göre sürebilir)...");

            string listFilePath = Path.Combine(tempFolder, "concat_list.txt");
            using (StreamWriter sw = new StreamWriter(listFilePath))
            {
                for (int i = 0; i < keepSegments.Count; i++)
                {
                    double start = keepSegments[i].Item1;
                    double duration = keepSegments[i].Item2 - start;
                    string chunkName = $"chunk_{i:D4}.mp4";
                    string chunkPath = Path.Combine(tempFolder, chunkName);

                    // Parçayı Kesme Komutu (Senkron bozulmasın diye videoyu yeniden kodluyoruz)
                    string cutArgs = $"-y -i \"{videoFile}\" -ss {start} -t {duration} -c:v libx264 -preset ultrafast -c:a aac -b:a 192k \"{chunkPath}\"";
                    RunFFmpeg(cutArgs);

                    sw.WriteLine($"file '{chunkName}'");
                    Console.Write($"\r   -> Parça {i + 1}/{keepSegments.Count} kesildi...");
                }
            }
            Console.WriteLine();

            // AŞAMA 4: PARÇALARI TEK VİDEO OLARAK BİRLEŞTİR
            Console.WriteLine("🔗 4. Aşama: Parçalar Senkronlu Şekilde Birleştiriliyor...");
            string concatArgs = $"-y -f concat -safe 0 -i \"{listFilePath}\" -c copy \"{finalOutputFile}\"";
            RunFFmpeg(concatArgs);

            // Temizlik
            Console.WriteLine("🧹 5. Aşama: Geçici dosyalar temizleniyor...");
            Directory.Delete(tempFolder, true);
            Directory.CreateDirectory(tempFolder); // Sonraki video için yeniden oluştur

            Console.WriteLine($"\n✅ MUAZZAM İŞ! Editlenmiş video hazır: {finalOutputFile}\n");
        }

        // Program sonu temizliği
        if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);

        Console.WriteLine("🎉 Tüm videolar işlendi. Kapatmak için bir tuşa bas...");
        Console.ReadKey();
    }

    // FFmpeg'i sessizce çalıştıran yardımcı fonksiyon
    static void RunFFmpeg(string arguments)
    {
        using (Process process = new Process())
        {
            process.StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            process.Start();
            process.WaitForExit();
        }
    }
}