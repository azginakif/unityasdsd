# Unity Multiplayer Arastirma Notlari

Tarih: 2026-04-28

## Ozet
- Unity 6 icin guncel oneri, eski tekil Relay/Lobby SDK'lari yerine `com.unity.services.multiplayer` kullanmak.
- Bu proje su an calisir bir gecis yolu olarak `Netcode for GameObjects + Unity Transport + Authentication + standalone Relay` yapisini kullaniyor.
- Kisa vadede bu kabul edilebilir bir prototip tabani. Uzun vadede lobby, session ve join-code akislarini MPS SDK uzerine tasimak daha dogru.

## Bu projeye etkisi
- Mevcut `RelayNetworkBootstrap` korunuyor; boylece mevcut oyun akisi bozulmuyor.
- UI tarafinda backend'in gecislik Relay yolu oldugu artik gorunur.
- Sonraki teknik adim: `RelayNetworkBootstrap` sinifini MPS Session tabanli yeni bir bootstrap'a ayirmak.

## Kaynaklar
- Unity Multiplayer Services SDK genel bakis: https://docs.unity.com/en-us/mps-sdk
- Unity Relay + NGO, unified package akisi: https://docs.unity.com/en-us/relay/relay-and-ngo
- Unity Multiplayer Services SDK kurulum ve gecis notu: https://docs.unity.com/en-us/mps-sdk/install-and-upgrade
- Unity MPS get started: https://docs.unity.com/en-us/mps-sdk/get-started
- UI referansi olarak Kenney UI Pack: https://kenney.nl/assets/ui-pack
