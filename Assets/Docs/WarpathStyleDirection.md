# Turan Strateji Style Direction

Bu not, eklenen referans gorsellerindeki oyun ritmini Turan Strateji icin sabitler.

## Ana Hedef

- Ust ekran: kaynaklar ince, yatay, okunakli ve her zaman sabit.
- Sol ust: profil portresi, oyuncu adi, guc ve seviye.
- Sag alt: ana menuler; Subaylar, Birimler, Ikmal, Ittifak, Ordu.
- Saha: secili birlik kartlari altta, birliklerin ustunde subay rozeti.
- Us: binalar net etiketli, bina adlari siyah yarim saydam etiketlerle okunur.
- Envanter: sol kategori listesi, orta grid, sag detay paneli.
- Birimler: sol kuvvet kategorileri, orta kart grid, kartta seviye/yildiz/adet.
- Subaylar: solda subay portre listesi, ortada buyuk karakter/portre, sagda guc, rol, seviye, beceri ve biyografi.

## Sahada Subay Kullanimi

- Her savas timi bir kisladan gelir.
- Kisladaki `assignedOfficer`, sahaya cikan birligin ustunde rozet olarak gorunur.
- Rozette portre, isim ve seviye bulunur.
- Subayin 3D karakteri sart degil; sahada rozet, Subaylar ekraninda portre/biyografi yeterlidir.

## Tim ve Silah Mantigi

- Tim datasinda savas degerleri bulunur.
- Silah ayri `WeaponData` assetidir.
- Karakter gorunumu ayri `UnitVisualProfile` assetidir.
- Her silaha/tim seviyesine gore farkli karakter prefabi kullanilir.
- Silah karakterin eline `WeaponSocket` uzerinden takilir.

## Model Uretim Kurali

- Tim karakteri: tek asker, A-pose veya T-pose, silahsiz, tam vucut.
- Silah: ayri model, elde veya karakterle birlikte degil.
- Subay: portre veya bust gorsel; biyografi ve beceri panelinde kullanilir.
- Gercek sehit/kahraman portreleri ticari yayin oncesi izin ve saygi denetiminden gecmelidir.

## UI Yoğunlugu

- Referans oyun gibi bilgi yogun olacak, fakat butonlar ekrandan tasmayacak.
- Mobil icin panel genislikleri ekranin %70-80'ini gecmeyecek.
- Alt menuler birlik slotlarini kapatmayacak.
- Metinler kutu icinde kalacak, tasma olursa satir kisaltilacak veya font kucultulecek.
