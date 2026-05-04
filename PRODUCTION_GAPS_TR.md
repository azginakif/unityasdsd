# MOBIL OFL - Gercek Yayina Hazirlik Eksikleri

Bu proje su an oynanis sistemleri olan bir prototip. Yayina hazir oyun icin eksikler sadece bug/derleme degil, icerik uretimidir.

## 1. Map / Level Design
- Final okul haritasi yok.
- Mevcut sahne blockout ve primitive objelerden olusuyor.
- Gerekli minimum vertical slice map:
  - Ana koridor
  - 1 oynanabilir sinif
  - Kutuphane
  - Guvenlik odasi
  - Ogretmenler odasi
  - Arsiv
  - Kantin
- Her bolgede oyuncuya okunur landmark, isik dili ve navigasyon ipucu olmali.
- Yeni arac: `Mobil OFL > Production > Generate Vertical Slice Blockout`
- Blockout odalari artik etkilesimli kapi objeleriyle uretiliyor.
- Arsiv kapisi `tool.archive-pass` ile gate ediliyor; bu, map'i vaka ilerlemesine baglayan ilk erisim ornegi.

## 2. Karakterler
- Final karakter modelleri yok.
- NPC'ler ve network oyuncu avatarlari artik kafa/govde/kol parcalarindan olusan placeholder visual ile uretiliyor.
- Bu sistem final model degil; rig ve animasyon gelene kadar okunurluk saglayan gecici temsil.
- Ilk gercek karakter paketi eklendi: `Assets/karakter/source/Final_SchoolBoy.fbx`.
- Yeni arac: `Mobil OFL > Characters > Setup School Boy Character`; model import, animator controller ve prefab uretir.
- Gerekli karakter seti:
  - Oyuncu dedektif avatar varyasyonlari
  - Guvenlik gorevlisi
  - Kutuphane ogrencisi
  - Ogretmen yardimcisi
  - Arsiv sorumlusu
  - Kantin calisani
  - Bilisim kulubu ogrencisi
- Yeni placeholder gorsel animasyon: `PlaceholderCharacterVisual`.

## 3. Animasyon
- Final rig, animator controller ve animasyon klipleri yok.
- Minimum animasyon paketi:
- Idle
- Walk
- Run
- Interact
- Talk
- Suspicious/alert
- Search/inspect
- Ziplama ve egilme MVP hareket setinden cikarildi; mobil kontrolde bu butonlar gizleniyor.

## 4. Modeller ve Prop Seti
- Final okul prop kit'i yok.
- Minimum prop seti:
  - Sira, sandalye, dolap, pano
  - Raf, kitap, kutu, evrak
  - Guvenlik terminali, kamera kaydi ekrani
  - Kantin tezgahi, kasa, enerji icecegi
  - Delil objeleri: not kagidi, anahtar, kart, dosya, terminal log'u

## 5. Ses
- Ortam sesi yok.
- UI sesleri final degil.
- Delil toplama, ekipman alma, NPC konusmasi, takim notu, tarama ve vaka sonucu icin procedural feedback tonlari eklendi.
- Adim, kapi, ortam, gerilim ve karakter sesleri henuz yok.
- Mobil cihazda kulakliksiz da okunabilir mix hazirlanmali.

## 6. UI / UX
- UI prototip olarak islevsel ama final polish yok.
- Mobil safe area, kucuk ekran, farkli aspect ratio ve dokunmatik ergonomi test edilmeli.
- Ilk onboarding sistemi eklendi: tarama, dosya, sorgu, ekipman ve final karar mesajlari otomatik veriliyor.
- Buna ragmen final tutorial flow'u henuz sinematik/etkilesimli ders seviyesinde degil.

## 7. Online
- Relay/Netcode prototipi var ama production session/lobby akisi eksik.
- Solo/offline ilerleme icin otomatik kayit eklendi; online oturum aktifken yerel kayit karistirilmiyor.
- Gercek cihazlarda:
  - Host kurma
  - Join code ile katilma
  - Kopma ve reconnect
  - Delil/NPC/not senkronizasyonu
  - Oyuncu hazirlik akisi
  test edilmeli.

## 8. Store / Build
- Android ikon, splash, keystore, version code hazir degil.
- Privacy policy, permission gerekceleri ve store metinleri hazir degil.
- Release build profili, IL2CPP ve cihaz performansi test edilmeli.

## Hemen Yapilan Ilk Uretim Adimlari
- Runtime eski sahne onarimi eklendi.
- Release validator eklendi.
- Mobil runtime FPS/uyku ayari eklendi.
- Placeholder karakter animasyon scripti eklendi.
- Vertical slice blockout generator eklendi.
- Procedural feedback audio sistemi eklendi.
- Etkilesimli kapilar ve kart gerektiren arsiv erisim gate'i eklendi.
- Ayrintili vaka checklist'i ve onboarding hint director eklendi.
- Solo/offline otomatik kayit ve geri yukleme sistemi eklendi.
