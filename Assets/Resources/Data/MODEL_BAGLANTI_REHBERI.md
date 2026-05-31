# Turan Model Baglanti Rehberi

Bu projede birlikler tek bir hierarchy objesi olarak tutulmaz. Her birlik veri assetidir.

## Birim modeli nereye konur?

1. Model dosyasini proje icinde uygun klasore koy:
   - Piyade modelleri: `Assets/Models/Units/Infantry/`
   - Tank modelleri: `Assets/Models/Units/Tanks/`
   - Topcu modelleri: `Assets/Models/Units/Artillery/`
   - Roket bataryasi modelleri: `Assets/Models/Units/RocketArtillery/`
   - Lojistik modelleri: `Assets/Models/Units/Logistics/`

2. Unity icinde modelden prefab olustur:
   - Piyade prefableri: `Assets/Prefabs/Units/Infantry/`
   - Tank prefableri: `Assets/Prefabs/Units/Tanks/`
   - Topcu prefableri: `Assets/Prefabs/Units/Artillery/`
   - Roket prefableri: `Assets/Prefabs/Units/RocketArtillery/`
   - Lojistik prefableri: `Assets/Prefabs/Units/Logistics/`

3. Ilgili birim assetini sec:
   - Baslangic piyade: `Assets/Resources/Data/Units/Turan/UD_bozkir_timi.asset`
   - Turkiye piyade: `Assets/Resources/Data/Units/Turkiye/UD_alp_timi.asset`
   - Tank: `Assets/Resources/Data/Units/Turkiye/UD_bozkurt_zirhli_timi.asset`
   - Topcu: `Assets/Resources/Data/Units/Turkiye/UD_boran_bataryasi.asset`
   - Roket: `Assets/Resources/Data/Units/Turkiye/UD_sakarya_roket_timi.asset`

4. Inspector alanlari:
   - `Model Baglantilari > Unit Model Prefab`: asker/tank/top/roket aracinin ana modeli.
   - `Model Baglantilari > Deployed Model Prefab`: haritada farkli model kullanilacaksa.
   - `Kisla Gosterimi > Barracks Visible Soldier Count`: kislada kac model gorunsun.
   - `Kisla Gosterimi > Visible Soldiers Per Barracks Level`: kisla level basina ek gorunen model.

## Silah modeli nereye konur?

1. Silah modelini koy:
   - Tufekler: `Assets/Models/Weapons/Rifles/`
   - Keskin nisanci: `Assets/Models/Weapons/Snipers/`
   - Tank namlusu/modulu: `Assets/Models/Weapons/TankGuns/`
   - Obus/top: `Assets/Models/Weapons/Howitzers/`
   - Roket sistemi: `Assets/Models/Weapons/Rockets/`

2. Ilgili `WeaponData` assetini sec:
   - Ornek: `Assets/Resources/Data/Weapons/Turan/WP_bozkir_timi.asset`
   - Ornek: `Assets/Resources/Data/Weapons/Turkiye/WP_boran_bataryasi.asset`

3. Inspector alanlari:
   - `Prefab > Weapon Prefab`: silah model prefabini buraya koy.
   - `Socket Local Position/Rotation/Scale`: elde veya arac uzerinde durus ayari.

## Yeni silah veya yeni tim ekleme mantigi

Yeni bir tim icin yeni `TuranUnitData` asseti acilir. Yeni silah icin yeni `WeaponData` asseti acilir. Birim assetindeki `Weapon Data` alani bu silaha baglanir. Kod yazmadan yeni birim ekleme yolu budur.

`Turan Strateji/Data/Build Starter Databases` menusu mevcut baslangic verilerini yeniden olusturur. Bu menu assetleri silmez, eksikleri tamamlar; ama elle verdigin model referanslarini korumak icin model alanlari bos degilse dokunmaz.
