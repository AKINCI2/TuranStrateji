# Turan Birlik Model Plani - Faz 1

Faz 1'de sadece piyade ve tank birlikleri kullanilacak. Topcu, roket, hava ve lojistik birlikleri oyun dongusu oturduktan sonra eklenecek.

## 1. Adlandirma Kurali

Birlik timleri artik soyut kahraman adlariyla degil, kullandigi silah veya arac platformuyla adlandirilacak.

Dogru ornekler:

- `MPT-55 Timi`
- `SAR 56 Timi`
- `MPT-76 Timi`
- `Altay Timi`
- `Kaplan MT Timi`

Yanlis ornekler:

- `Alp Timi`
- `Akinci Timi`
- `Bozkir Timi`

## 2. Piyade Birlikleri - Faz 1

| Guc | Oyun Adi | UnitData | WeaponData | Model Notu |
| --- | --- | --- | --- | --- |
| 1 | MPT-55 Timi | `UD_mpt55_timi` | `WP_mpt55` | Hafif modern piyade, 5.56 mm tufek, 6 asker |
| 2 | SAR 56 Timi | `UD_sar56_timi` | `WP_sar56` | Daha modern piyade, SAR 56, 6 asker |
| 3 | MPT-76 Timi | `UD_mpt76_timi` | `WP_mpt76` | Daha agir piyade, 7.62 mm MPT-76, 6-8 asker |

Baslangic icin bu uc piyade yeterli. Yeni piyade silahi eklendiginde yeni bir `TuranUnitData` ve yeni bir `WeaponData` olusturulur.

## 3. Tank Birlikleri - Faz 1

| Guc | Oyun Adi | UnitData | WeaponData | Model Notu |
| --- | --- | --- | --- | --- |
| 1 | Kaplan MT Timi | `UD_kaplan_mt_timi` | `WP_kaplan_mt_105mm` | Orta sinif tank, 1 arac |
| 2 | Altay Timi | `UD_altay_timi` | `WP_altay_120mm` | Ana muharebe tanki, 1 arac |

Baslangicta iki tank yeterli. Oyun oturduktan sonra modernizasyon seviyeleri ayri birlik olarak acilabilir.

## 4. Tank WeaponData Ne Demek?

Tank icin `WeaponData`, tank namlusunun mutlaka ayri model olmasi demek degildir.

Faz 1'de tank modeli tek parca prefab olabilir:

```text
AltayTank.prefab
  Tank govdesi
  Kule
  Namlu
  MuzzlePoint
```

Bu durumda:

- `TuranUnitData > Unit Model Prefab` alanina komple tank prefabini koy.
- `WeaponData > Weapon Prefab` bos kalabilir.
- `WeaponData` sadece hasar, menzil, atis hizi ve zirh delme degerlerini tutar.

Faz 2'de kule/namlu ayri kontrol edilecekse:

- Namlu veya kule silah modulu ayri prefab yapilir.
- `WeaponData > Weapon Prefab` alanina bu modul koyulur.
- Tank prefabinda `WeaponSocket`, `MuzzlePoint`, `TurretSocket` child objeleri kullanilir.

Yani bugun icin tank namlusunu ayri yapmak zorunda degilsin.

## 5. Modeli Nereye Koyacaksin?

### Piyade modeli

Ham model:

```text
Assets/Models/Units/Infantry/MPT55_Soldier.fbx
Assets/Models/Units/Infantry/SAR56_Soldier.fbx
Assets/Models/Units/Infantry/MPT76_Soldier.fbx
```

Prefab:

```text
Assets/Prefabs/Units/Infantry/MPT55_Soldier.prefab
Assets/Prefabs/Units/Infantry/SAR56_Soldier.prefab
Assets/Prefabs/Units/Infantry/MPT76_Soldier.prefab
```

Baglanti:

```text
TuranUnitData > Model Baglantilari > Unit Model Prefab
```

### Piyade silah modeli

Ham model:

```text
Assets/Models/Weapons/Rifles/MPT55.fbx
Assets/Models/Weapons/Rifles/SAR56.fbx
Assets/Models/Weapons/Rifles/MPT76.fbx
```

Prefab:

```text
Assets/Prefabs/Weapons/Rifles/MPT55.prefab
Assets/Prefabs/Weapons/Rifles/SAR56.prefab
Assets/Prefabs/Weapons/Rifles/MPT76.prefab
```

Baglanti:

```text
WeaponData > Prefab > Weapon Prefab
```

### Tank modeli

Ham model:

```text
Assets/Models/Units/Tanks/KaplanMT.fbx
Assets/Models/Units/Tanks/Altay.fbx
```

Prefab:

```text
Assets/Prefabs/Units/Tanks/KaplanMT.prefab
Assets/Prefabs/Units/Tanks/Altay.prefab
```

Baglanti:

```text
TuranUnitData > Model Baglantilari > Unit Model Prefab
```

## 6. Piyade Animasyon Baglantisi

Piyade modeli Humanoid olmalidir.

Import ayari:

- Rig: `Humanoid`
- Avatar Definition: `Create From This Model`
- Model yonu: Z+
- Pivot: ayaklarin ortasi, zemin
- Boy: yaklasik 1.75-1.9 Unity unit

Prefab yapisi:

```text
MPT76_Soldier.prefab
  Animator
  SkinnedMeshRenderer
  WeaponSocket
```

`WeaponSocket`, sag elin altinda bos bir child obje olmali. Silah modeli buraya takilir.

Animator Controller parametreleri:

- `Speed` float
- `Attack` trigger
- `Hit` trigger
- `Dead` bool

Minimum animasyonlar:

- Idle
- Walk
- Run
- Attack
- Hit
- Death

Baglanti:

1. `Assets/Animations/Controllers/Infantry.controller` olustur.
2. Asker prefabindaki `Animator` alanina bu controlleri koy.
3. Ilgili `UnitVisualProfile` assetinde `Animator Controller` alanina da ayni controlleri koy.
4. Ilgili `TuranUnitData` assetinde `Unit Visual Profile` alaninin dolu oldugundan emin ol.

## 7. Tank Animasyon Fikri

Faz 1'de tank animasyonu zorunlu degil. Tank statik model olarak hareket edebilir.

Faz 2 icin:

- Kule hedefe dogru doner.
- Namlu yukari-asagi aim yapar.
- Atis aninda namlu geri teper.
- Muzzle flash `MuzzlePoint` konumunda cikar.
- Palet/teker materyali kaydirilarak hareket hissi verilir.

## 8. Topcu Icin Sonraki Faz Animasyon Fikirleri

Topcu simdilik ertelendi, cunku askerlerin topu itmesi icin ek animasyon gerekiyor.

Sonraki fazda secenekler:

- Cekili top icin 2 asker itme animasyonu.
- Topu kurma animasyonu: ayaklar acilir, namlu yukselir.
- Mermi yukleme animasyonu: asker mermi tasir ve topa yerlestirir.
- Atis sonrasi namlu tepmesi ve duman.
- Kundağı motorlu top kullanilirsa insan itme animasyonu gerekmez, arac modeli yeterli olur.

## 9. Unity Asset Baglanti Ornegi

MPT-76 Timi:

```text
UnitData: Assets/Resources/Data/Units/Turkiye/UD_mpt76_timi.asset
Unit Model Prefab: Assets/Prefabs/Units/Infantry/MPT76_Soldier.prefab
WeaponData: Assets/Resources/Data/Weapons/Turkiye/WP_mpt76.asset
Weapon Prefab: Assets/Prefabs/Weapons/Rifles/MPT76.prefab
```

Altay Timi:

```text
UnitData: Assets/Resources/Data/Units/Turkiye/UD_altay_timi.asset
Unit Model Prefab: Assets/Prefabs/Units/Tanks/Altay.prefab
WeaponData: Assets/Resources/Data/Weapons/Turkiye/WP_altay_120mm.asset
Weapon Prefab: bos kalabilir
```
