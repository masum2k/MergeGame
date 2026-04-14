# Google Play Release Checklist (game3)

Bu dokuman, bu proje icin Google Play yayini oncesi teknik kontrol listesidir.

## 1. Kimlik ve Sürüm

1. Company Name ve Product Name degerlerini final marka adina cek.
2. Android package name'i benzersiz yap (ornek: com.sirketadi.game3).
3. Version (bundleVersion) ve Version Code (AndroidBundleVersionCode) degerlerini arttir.
4. Version code'un her yuklemede tekil ve artan oldugunu dogrula.

## 2. Android Build Zorunluluklari

1. Scripting Backend: IL2CPP.
2. Architecture: ARM64 aktif.
3. Build format: Android App Bundle (.aab).
4. Min SDK: API 24+.
5. Target SDK: Google Play'in guncel zorunluluguna uygun.
6. Release minify (R8/Proguard) ve stripping seviyesini test ederek aktif et.

## 3. Oyun Icindeki Mobil Stabilite

Bu repo icinde uygulananlar:

1. Merkezi kaydetme koordinasyonu eklendi: Assets/Scripts/Managers/SaveCoordinator.cs
2. Mobil runtime bootstrap eklendi: Assets/Scripts/Managers/MobileRuntimeBootstrap.cs
3. Safe area uygulamasi eklendi: Assets/Scripts/UI/SafeAreaFitter.cs
4. Envanter kaliciligi eklendi: Assets/Scripts/Managers/InventoryManager.cs
5. Surekli UI yenilemeleri throttle edildi: MarketScreenPage ve FactoryScreenPage.

Test edilmesi gerekenler:

1. Uygulamayi arka plana alip geri dondugunde veri kaybi olmamasi.
2. Cihaz notch/delik ekranlarda UI tasmasi olmamasi.
3. 2 GB, 4 GB ve 8 GB RAM sinifinda FPS/sicaklik davranisi.
4. Uzun sureli oyun (30+ dk) sonrasi bellek ve akicilik.

## 4. Play Console Hazirlik

1. Privacy Policy URL hazirla ve Play Console'a gir.
2. Data safety formu doldur.
3. Icerik derecelendirme (content rating) anketini tamamla.
4. App access formunu doldur (gerekliyse test hesabi ver).
5. Reklam kullaniyorsan Ads deklarasyonunu dogru isaretle.

## 5. Store Listing Asset'leri

1. 512x512 app icon
2. 1024x500 feature graphic
3. En az 2 telefon screenshot'u
4. Varsa tablet screenshot'u
5. Kisa aciklama + tam aciklama + lokalizasyon metinleri

## 6. Preflight Araci

Editor menusu:

1. Tools -> Release -> Google Play Preflight Report

Bu arac package, IL2CPP, ARM64, app bundle, min SDK, minify ve aktif hedef platformu kontrol eder.

## 7. Son Onay Akisi

1. Development Build kapali final AAB al.
2. Gercek cihazlarda smoke test yap:
   - Ilk acilis
   - Sandik acma
   - Envanter secimi
   - Merge + ilerleme
   - Arka plan/geri donus
3. Internal testing track'e yukle.
4. Crash/ANR yoksa Closed testing -> Production gec.