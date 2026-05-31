# Turan Base Model Requirements

Bu liste us sisteminin sabit parsel duzenine gore hazirlandi. Her bina modeli kendi klasorune konacak, sonra prefab olarak baglanacak.

## Hazir Olanlar

- HQ Lv1 model: `Assets/Models/Buildings/HQ/Lv1/Hqlv1.fbx`
- HQ Lv1 prefab: `Assets/Prefabs/Buildings/HQ/HQ_Level1_Visual.prefab`
- Uretim Tesisi Lv1 model klasoru: `Assets/Models/Buildings/ProductionFacility/Level1/`
- Uretim Tesisi Lv1 prefab: `Assets/Prefabs/Buildings/ProductionFacility/UretimTesisi_Level1.prefab`

## Uretilecek Sirali Modeller

1. Kisla Lv1
   - Model: `Assets/Models/Buildings/Barracks/Lv1/Barracks_Lv1.fbx`
   - Prefab: `Assets/Prefabs/Buildings/Barracks/Barracks_Level1.prefab`
   - Parsel: `slot_barracks_01`
   - Kilit: HQ Lv1
   - Brief: Askeri yatakhane, talim avlusu, bayrak/arma alani, modern Turk/Turan askeri dili.

2. Depo Lv1
   - Model: `Assets/Models/Buildings/Warehouse/Lv1/Warehouse_Lv1.fbx`
   - Prefab: `Assets/Prefabs/Buildings/Warehouse/Warehouse_Level1.prefab`
   - Parsel: `slot_warehouse_01`
   - Kilit: HQ Lv2
   - Brief: Lojistik hangari, konteynerler, yukleme rampasi, kaynak kasalari.

3. Arastirma Ussu Lv1
   - Model: `Assets/Models/Buildings/ResearchCenter/Lv1/ResearchCenter_Lv1.fbx`
   - Prefab: `Assets/Prefabs/Buildings/ResearchCenter/ResearchCenter_Level1.prefab`
   - Parsel: `slot_research_01`
   - Kilit: HQ Lv3
   - Brief: Radar kubbesi, laboratuvar bloklari, antenler, daha temiz ve teknolojik siluet.

4. Celik Fabrikasi Lv1
   - Model: `Assets/Models/Buildings/SteelFactory/Lv1/SteelFactory_Lv1.fbx`
   - Prefab: `Assets/Prefabs/Buildings/SteelFactory/SteelFactory_Level1.prefab`
   - Parsel: `slot_steel_01`
   - Kilit: HQ Lv4
   - Brief: Baca, celik stok alani, kucuk vinc kolu, endustriyel ama mobil ekranda okunakli form.

5. Petrol Rafinerisi Lv1
   - Model: `Assets/Models/Buildings/OilRefinery/Lv1/OilRefinery_Lv1.fbx`
   - Prefab: `Assets/Prefabs/Buildings/OilRefinery/OilRefinery_Level1.prefab`
   - Parsel: `slot_oil_01`
   - Kilit: HQ Lv5
   - Brief: Tanklar, boru hatlari, pompa istasyonu, sari/siyah guvenlik detaylari.

6. Bor Tesisi Lv1
   - Model: `Assets/Models/Buildings/BorMine/Lv1/BorMine_Lv1.fbx`
   - Prefab: `Assets/Prefabs/Buildings/BorMine/BorMine_Level1.prefab`
   - Parsel: `slot_bor_01`
   - Kilit: HQ Lv6
   - Brief: Cevher bunkerleri, konveyor, bor isleme unitesi, mavi-beyaz mineral vurgusu.

7. Kisla Lv2
   - Model: `Assets/Models/Buildings/Barracks/Lv2/Barracks_Lv2.fbx`
   - Prefab: `Assets/Prefabs/Buildings/Barracks/Barracks_Level2.prefab`
   - Parsel: mevcut kisla parselleri
   - Kilit: HQ Lv7
   - Brief: Daha buyuk talim sahasi, ikinci kat/ek blok, daha guclu askeri kimlik.

## Teknik Model Kurallari

- Pivot: modelin alt merkezinde olmali.
- Forward: Unity +Z yonune bakmali.
- Scale: 1 Unity unit = 1 metre kabul edilecek.
- Model siniri: parselden tasmamali; buyuk binalar yaklasik 1.9 x 1.45 metre tabana sigmali.
- Poly hedefi: mobil icin Lv1 bina basina 2k-8k triangle arasi.
- Texture: 1 adet ana atlas tercih edilir, 1024 veya 2048 boyut yeterli.
- Dosya duzeni: FBX, texture ve materyaller ayni bina seviye klasorunde dursun.
- Baglama: Prefab olusturulduktan sonra ilgili `BuildingData.levels[level - 1].visualPrefab` alanina atanacak.
