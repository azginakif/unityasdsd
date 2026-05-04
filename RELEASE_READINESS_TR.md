# MOBIL OFL - Yayina Hazirlik Kontrol Listesi

## Mevcut Durum
- Proje oynanabilir prototip seviyesinde.
- Ana vaka, delil toplama, NPC sorgu, vaka dosyasi, mobil kontrol ve online prototip altyapisi mevcut.
- Yayina cikmadan once sahne butunlugu, mobil build, online oturum ve oynanis dongusu Unity Editor icinde dogrulanmali.

## Unity Icinde Ilk Calistirilacak Kontroller
1. `Mobil OFL > Validation > Run Release Readiness Check`
2. Hata varsa: `Mobil OFL > Validation > Repair Active Scene Release Blockers`
3. `Mobil OFL > Setup > Auto Setup Investigation Scene`
4. Play Mode test: solo akisi bastan sona bitir.
5. Play Mode test: offline host ile network avatar spawn akisini kontrol et.

## Release Blokajlari
- Sahne eski uretilmis olabilir; ekipman pickup objeleri eksikse arama akisi kilitlenir.
- Android cihazda dokunmatik kontrol, UI safe area ve performans test edilmedi.
- Relay online akisinda gercek cihazlar arasi join-code testi yapilmadi.
- Store yayini icin ikon, splash, gizlilik metni, uygulama adi, imza/keystore ve build numarasi hazir degil.
- Gercek okul modeli, final sanat kalitesi, ses ve polish henuz prototip seviyesinde.

## Kapatilan Ilk Teknik Riskler
- Eski sahnede arama noktalari ekipmansiz calisiyorsa runtime gate onarimi eklendi.
- Eksik `tool.archive-pass` ve `tool.lockpick` pickup objeleri runtime'da otomatik ekleniyor.
- Unity menu uzerinden release readiness validator eklendi.
- NPC ve online oyuncu avatarlari icin placeholder karakter visual uretimi eklendi.
- Ilk Sketchfab/Mixamo okul cocugu karakter pipeline'i eklendi; prefab/controller uretimi `Mobil OFL > Characters > Setup School Boy Character` menusuyle yapiliyor.
- Delil, ekipman, NPC, not, tarama ve sonuc olaylari icin procedural ses feedback'i eklendi.
- Vertical slice blockout odalari icin etkilesimli kapi ve arsiv karti gate'i eklendi.
- Onboarding hint sistemi eklendi; ilk delil, tarama, dosya, sorgu, ekipman ve final suclama adimlarinda oyuncuya baglamsal mesaj verir.
- HUD checklist'i siradaki acik vaka adimlarini gosterecek sekilde genisletildi.
- Solo/offline vaka ilerlemesi otomatik kaydedilip sonraki acilista geri yukleniyor.

## Sonraki Sprint Sirasi
1. Unity 6000.4.3f1 ile import ve compile hatalarini temizle.
2. Validator sonucundaki tum hata maddelerini sifirla.
3. Solo vaka akisini 10 dakikalik oynanabilir bolum olarak dengele.
4. Kayit geri yukleme akisini Play Mode'da kapat/ac senaryosuyla test et.
5. Android build al, cihazda FPS, UI ve kontrol testi yap.
6. Relay ile iki cihaz / iki editor instance online test yap.
7. Ana menu, sonuc ekrani, hata mesajlari ve oyuncu onboarding metinlerini polish et.
8. Store hazirligi: ikon, splash, package id, keystore, privacy policy, build version.
