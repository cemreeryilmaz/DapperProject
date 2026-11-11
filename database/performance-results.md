# SalesPulse performans ölçümü

Ölçüm zamanı: 05.11.2026 17:41

Koşullar:

- Veri hacmi: 2.000.000 sipariş, 3.500.000 kalem, 100.000 müşteri.
- Aralık: 1 Kasım 2025 (dahil) - 6 Kasım 2026 (hariç).
- Sipariş sayfası sorgusu: İstanbul (`CityId = 34`), durum `Tamamlandı`, `OFFSET 0 FETCH 25`.
- Her sorgu iki kez çalıştırıldı. Süre ve okuma, derleme etkisini ayırmak için ikinci çalıştırmadan alındı.
- Önce ölçümünde yalnızca birincil anahtar ve tekillik kısıtlarının indeksleri vardı. Sonra ölçümünde `03_indexes.sql` indeksleri eklendi.
- Mantıksal okuma `sys.dm_exec_query_stats.last_logical_reads` değeridir; istatistik mesajı yedek olarak kullanılır.
- İptal hariç ciro kuralı müşteri sorgusunda uygulanır. Aylık eğilim sorgusu ham toplamı ölçer.

SQL Server:

```
Microsoft SQL Server 2022 (RTM-GDR) (KB5128493) - 16.0.1210.3 (X64) 
	Oct  2 2026 17:33:48 
	Copyright (C) 2022 Microsoft Corporation
	Express Edition (64-bit) on Windows 10 Pro 10.0 <X64> (Build 26300: ) (Hypervisor)
```

| Sorgu | Önce (ms) | Sonra (ms) | Önce okuma | Sonra okuma | Okuma farkı | Plan (sonra) |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Sipariş sayfası (tarih + şehir + durum, OFFSET/FETCH) | 432 | 1 | 20.469 | 180 | 20.289 | Top, Nested Loops, Clustered Index Seek, Merge Interval, Concatenation, Compute Scalar, Constant Scan, Index Seek |
| Aylık satış eğilimi (GROUP BY) | 964 | 388 | 20.161 | 7.178 | 12.983 | Sort, Hash Match, Compute Scalar, Nested Loops, Merge Interval, Concatenation, Constant Scan, Index Seek |
| En yüksek cirolu müşteriler (JOIN, GROUP BY, HAVING) | 1.020 | 539 | 20.586 | 7.262 | 13.324 | Top, Filter, Nested Loops, Sort, Hash Match, Merge Interval, Concatenation, Compute Scalar |

İndeksler ve gerekçeleri:

- `IX_Orders_OrderDate_Status` — tarih aralığı, aylık gruplama ve durum filtresi.
- `IX_Orders_City_OrderDate` — şehir + tarih sayfalama.
- `IX_Orders_Customer_OrderDate` — müşteri cirosu ve tekrar alışveriş oranı.
- `IX_Customers_FullName` — sipariş no / müşteri ön ek araması.
- `IX_OrderDetails_OrderId` ve `IX_OrderDetails_ProductId` — sipariş detayı ile ürün/kategori cirosu.
- `IX_Products_CategoryId` — kategori kırılımı.
- `IX_Payments_OrderId_Cover` — ödeme yöntemi için anahtar aramasını kapatan kapak indeks.

Önce plan özetleri:

- Sipariş sayfası (tarih + şehir + durum, OFFSET/FETCH): Top, Nested Loops, Clustered Index Seek, Sort, Clustered Index Scan, Index Seek
- Aylık satış eğilimi (GROUP BY): Sort, Hash Match, Compute Scalar, Clustered Index Scan
- En yüksek cirolu müşteriler (JOIN, GROUP BY, HAVING): Top, Filter, Nested Loops, Sort, Hash Match, Clustered Index Scan, Clustered Index Seek
