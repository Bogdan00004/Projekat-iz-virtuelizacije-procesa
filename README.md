# Meteorološka Stanica - WCF Projekat

## Tim
| Indeks | Ime | Prezime |
|--------|-----|---------|
| PR 149/2023 | Bogdan | Pećanac |
| PR 153/2023 | Aleksandar | Petrović |

## O projektu
Projekat implementira sistem za praćenje i analizu vremenskih parametara
korišćenjem WCF servisa i događajnog modela u C# (.NET Framework).

## Tehnologije
- C# / .NET Framework
- WCF (Windows Communication Foundation)
- netTcpBinding
- FileStream / StreamWriter / MemoryStream
- Delegati i događaji

## Struktura projekta
MeteorologijaProjekat/
├── Common/        → Interfejsi, DataContracts, Faultovi
├── Server/        → WCF servis, logika, događaji, analitika
└── Client/        → CSV čitanje, slanje podataka ka servisu

## Pokretanje
1. Pokrenuti **Server** projekat
2. Pokrenuti **Client** projekat

## Zadaci
-  Zadatak 1 - Skica arhitekture i pravila protokola
-  Zadatak 2 - WCF servis, konfiguracija i ugovori
-  Zadatak 3 - Validacija podataka
-  Zadatak 4 - Dispose pattern
-  Zadatak 5 - CSV učitavanje na klijentu
-  Zadatak 6 - Snimanje fajlova na serveru
-  Zadatak 7 - Sekvencijalni streaming
-  Zadatak 8 - Delegati i događaji
-  Zadatak 9 - Analitika ΔSH
-  Zadatak 10 - Analitika ΔHI
