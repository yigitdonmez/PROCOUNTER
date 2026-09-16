# ProCounter 🍏🤖

AI-powered, cross-platform calorie and macro-nutrient tracker built with .NET MAUI and ASP.NET Core.

> 🇬🇧 [English](#-english) &nbsp;|&nbsp; 🇹🇷 [Türkçe](#-türkçe)

---

## 🎥 Demo

*(Add a short video or GIF here demonstrating the AI logging flow and the dynamic UI effects)*

<video src="https://github.com/yigitdonmez/PROCOUNTER/releases/download/v1.0.0/VID_20260916_181504.mp4" controls="controls" width="100%">
</video>

---

## 🇬🇧 English

### 📖 About The Project
**ProCounter** is a cross-platform, AI-powered calorie and macro-nutrient tracking mobile application. It replaces the traditional food-logging flow — searching for foods and entering portion sizes by hand — with natural language input. A user simply types what they ate, and an LLM extracts precise nutritional data instantly.

### ✨ Key Features
- **🧠 AI-Powered Natural Language Processing** — Powered by the **Gemini API**, the app parses complex, natural-language food descriptions and calculates calories, protein, carbs, and fat automatically.
- **🌍 True Dynamic Localization** — Seamless English/Turkish support via custom XAML `ValueConverter`s and `AppResources`. The UI language switches instantly, with no app restart required.
- **🎨 Modern, Responsive UI/UX**
  - A custom "Breathing Glow" effect that animates and changes color based on the user's daily calorie threshold (e.g., >500 kcal).
  - A clean, native-feeling interface achieved by overriding default platform behaviors (e.g., globally removing the native Android `Entry` underline via `EntryHandler` mapping).
- **☁️ Cloud-Native Architecture** — Backend deployed on **Render**, backed by a serverless **PostgreSQL** database hosted on **Neon**.
- **🔐 Secure Sessions** — JWT-based authentication for secure, private user data management.
- **📊 Advanced Data Management** — Full CRUD on meal entries: adjust portion weights (macros recalculate automatically), change meal type (breakfast, lunch, snack, etc.), and sort daily consumption by macro or calorie value.

### 🛠️ Technology Stack
| Layer | Technology |
|---|---|
| Frontend / Mobile | C#, .NET MAUI, XAML |
| Backend / API | ASP.NET Core Web API, Entity Framework Core, SignalR |
| Database | PostgreSQL (Neon Serverless) |
| Deployment | Render |
| AI Integration | Gemini API |
| Architecture | RESTful API, MVVM (mobile) |

### 🌐 Try It Live
The `CalorieApi` HttpClient registered in `MauiProgram.cs` already points to the live, deployed backend on Render — no need to run your own API or database to try the app.

1. Clone the repository.
2. Open the solution and build/run the `CalorieTracker.Mobile` project on an emulator or device (see [Prerequisites](#prerequisites) below).
3. That's it — the app connects to the live API automatically.

> ⚠️ This is a free-tier deployment: the first request may take 30–60 seconds to wake the service up (cold start), and usage is subject to Gemini API rate limits.

### 🚀 Getting Started

#### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022, or VS Code with the MAUI extension
- Android emulator or physical device

#### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/yigitdonmez/ProCounter.git
   ```
2. Open the solution in your IDE.
3. Configure your API keys and connection string as local secrets rather than committing them directly:
   - Backend: add your **Gemini API key** and **Neon PostgreSQL connection string** via `dotnet user-secrets` (or a local, git-ignored `appsettings.Development.json`).
   - Mobile: the base address is already configured in `MauiProgram.cs` (`AddHttpClient("CalorieApi", ...)`) to point to the live deployment — only change it if you want to run the backend locally instead.
4. Run the API project.
5. Build and run the `CalorieTracker.Mobile` project.

> ⚠️ Never commit real API keys or connection strings to `appsettings.json`. Use user secrets, environment variables, or a git-ignored config file instead.

---

## 🇹🇷 Türkçe

### 📖 Proje Hakkında
**ProCounter**, yapay zeka destekli, çapraz platform bir kalori ve makro besin takip mobil uygulamasıdır. Geleneksel manuel yemek arama ve porsiyon girme sürecini ortadan kaldırır. Kullanıcı yediğini doğal bir dille yazar, bir LLM bu metni işleyerek makro besin değerlerini anında ve hassas şekilde hesaplar.

### ✨ Öne Çıkan Özellikler
- **🧠 Yapay Zeka Destekli Doğal Dil İşleme** — **Gemini API** entegrasyonu sayesinde uygulama, karmaşık yemek tariflerini veya günlük konuşma dilindeki girdileri anlar; kalori, protein, karbonhidrat ve yağ değerlerini otomatik hesaplar.
- **🌍 Tam Dinamik Lokalizasyon** — XAML `ValueConverter` mimarisi ve `AppResources` kullanılarak İngilizce/Türkçe dil desteği eklenmiştir. Dil değişimi, uygulamayı yeniden başlatmadan anında gerçekleşir.
- **🎨 Modern ve Akıcı UI/UX**
  - Günlük kalori tüketimi belirli bir eşiği (örn. 500 kcal) aştığında devreye giren, renk değiştiren "Breathing Glow" animasyonu.
  - Android'in varsayılan alt çizgili `Entry` tasarımı gibi platforma özgü detaylar `EntryHandler` üzerinden ezilerek modern ve temiz bir arayüz elde edilmiştir.
- **☁️ Bulut Tabanlı Mimari** — Backend **Render** üzerinde barındırılır; veritabanı olarak **Neon** üzerinde çalışan sunucusuz **PostgreSQL** kullanılır.
- **🔐 Güvenli Oturum Yönetimi** — Kullanıcı verilerinin gizliliği için JWT tabanlı kimlik doğrulama uygulanmıştır.
- **📊 Gelişmiş Veri Yönetimi** — Öğünler üzerinde tam CRUD kontrolü: gramaj değiştirildiğinde makrolar otomatik yeniden hesaplanır, öğün tipi güncellenebilir, günlük tüketim listesi makro veya kaloriye göre sıralanabilir.

### 🛠️ Teknoloji Yığını
| Katman | Teknoloji |
|---|---|
| Önyüz / Mobil | C#, .NET MAUI, XAML |
| Arkayüz / API | ASP.NET Core Web API, Entity Framework Core, SignalR |
| Veritabanı | PostgreSQL (Neon Serverless) |
| Dağıtım | Render |
| Yapay Zeka Entegrasyonu | Gemini API |
| Mimari | RESTful API, MVVM (mobil) |

### 🌐 Canlı Olarak Deneyin
`MauiProgram.cs` içinde kayıtlı `CalorieApi` HttpClient'ı, Render üzerinde yayında olan gerçek backend'e zaten bağlı — uygulamayı denemek için kendi API'ni veya veritabanını çalıştırmana gerek yok.

1. Repository'yi klonla.
2. Solution'ı aç ve `CalorieTracker.Mobile` projesini bir emülatörde veya cihazda derleyip çalıştır (aşağıdaki [Gereksinimler](#gereksinimler) kısmına bak).
3. Bu kadar — uygulama canlı API'ye otomatik olarak bağlanır.

> ⚠️ Bu ücretsiz (free-tier) bir deployment: ilk istek servisi "uyandırmak" için 30–60 saniye sürebilir (cold start), ayrıca kullanım Gemini API rate limitlerine tabidir.

### 🚀 Kurulum ve Başlangıç

#### Gereksinimler
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 veya MAUI eklentili VS Code
- Android emülatörü veya fiziksel cihaz

#### Kurulum Adımları
1. Projeyi klonlayın:
   ```bash
   git clone https://github.com/yigitdonmez/ProCounter.git
   ```
2. Çözüm (solution) dosyasını IDE'nizde açın.
3. API anahtarlarını ve bağlantı bilgilerini doğrudan koda gömmek yerine yerel gizli anahtar olarak tanımlayın:
   - Backend: **Gemini API** anahtarınızı ve **Neon PostgreSQL** bağlantı cümlenizi `dotnet user-secrets` ile (veya git tarafından takip edilmeyen yerel bir `appsettings.Development.json` dosyasıyla) ekleyin.
   - Mobil: base address zaten `MauiProgram.cs` içinde (`AddHttpClient("CalorieApi", ...)`) canlı deployment'a bağlı olarak ayarlı — sadece backend'i lokal çalıştırmak istersen değiştir.
4. API projesini çalıştırın.
5. `CalorieTracker.Mobile` projesini derleyip çalıştırın.

> ⚠️ Gerçek API anahtarlarını veya bağlantı cümlelerini `appsettings.json` içine commit etmeyin. Bunun yerine user secrets, ortam değişkenleri veya git tarafından izlenmeyen bir config dosyası kullanın.

---

## 👤 Author
**Salih Yiğit Dönmez**
[GitHub](https://github.com/yigitdonmez) · [LinkedIn](https://www.linkedin.com/in/salih-yigit-donmez)

## 📄 License
**Copyright (c) 2026. All Rights Reserved.**
This project is proprietary and intended solely as a portfolio showcase. Unauthorized copying, modification, distribution, or use of this software, via any medium, is strictly prohibited.