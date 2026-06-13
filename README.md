# Conglomerate Tycoon 2

**Conglomerate Tycoon 2** to zaawansowana gra symulacyjna typu *tycoon*, napisana w języku C# z wykorzystaniem frameworka Windows Forms. Gracz wciela się w rolę prezesa konglomeratu przemysłowo-handlowego, zarządzając pełnym łańcuchem dostaw: od pozyskiwania surowców, przez produkcję w fabrykach, aż po sprzedaż detaliczną w sklepach.

---

## 🚀 Główne Funkcje Gry

### 1. Centralny Panel Logistyczny (Manager Floty 🚚)
* **Zarządzanie Trasami**: Dystrybucja towarów wewnątrz firmy lub automatyczne zakupy na Wolnym Rynku.
* **Flota i Klasy Pojazdów**: Do dyspozycji gracza są zróżnicowane typy pojazdów (np. szybki, mały *Van* oraz pojemny, lecz wolniejszy *Truck*).
* **Zaawansowane Reguły (Triggery)**:
  * Wysyłka tylko po pełnym załadowaniu (100% capacity).
  * Wysyłka przy częściowym załadowaniu (minimum 50%).
  * Wysyłka cykliczna na czas (interwały czasowe).
* **System Priorytetów**: Trasy z wysokim priorytetem automatycznie otrzymują pojazdy w pierwszej kolejności w przypadku limitów floty (maksymalnie 8 aktywnych pojazdów jednocześnie).
* **Logistics OPEX**: Koszty transportu są na bieżąco obliczane i potrącane z bilansu firmy.

### 2. Zaawansowany Silnik Finansowy
* **Rachunek Zysków i Strat (P&L)**: Śledzenie przychodów, kosztów surowcowych (COGS), logistyki, marketingu, płac i amortyzacji.
* **Bilans Przedsiębiorstwa (Balance Sheet)**: Pełna weryfikacja równania bilansowego (*Aktywa == Pasywa + Kapitał Własny*) na koniec każdego miesiąca.
* **Podział CAPEX / OPEX**: Inwestycje w środki trwałe (zakup nieruchomości/budynków) są poprawnie księgowane w bilansie i amortyzowane w czasie, nie destabilizując wyniku operacyjnego przedsiębiorstwa.
* **Raporty 24h placówek**: Szczegółowy monitoring rentowności każdego sklepu oraz wolumenu sprzedaży poszczególnych półek.

### 3. Mapa Izometryczna (Precyzja Kliknięć)
* Zastosowanie matematyki izometrycznej w przestrzeni 2D pozwala na **pixel-perfect zaznaczanie kafelków** i budynków bez ryzyka kliknięcia sąsiednich obiektów.

### 4. Wbudowana Obsługa Wyjątków
* Gra posiada dedykowany, ciemny formularz zgłaszania awarii (`ErrorDetailsForm`) zastępujący standardowe powiadomienia Windows, co pozwala na skopiowanie stack trace i bezpieczne kontynuowanie lub wyjście z gry.

---

## 🛠️ Wymagania Techniczne

* System operacyjny: Windows
* Środowisko uruchomieniowe: **.NET SDK 10.0** (lub nowsze)
* Zależności: MonoGame.Framework.WindowsDX (wbudowane w pakiet NuGet)

---

## 💻 Uruchamianie Projektu

### Uruchomienie Gry:
Możesz uruchomić grę klikając dwukrotnie na plik:
```bash
Uruchom_Gre.bat
```
lub wpisując w terminalu w głównym katalogu projektu:
```bash
dotnet run
```

### Uruchomienie Testów Systemowych:
Projekt zawiera dedykowany moduł testów weryfikujący stabilność ekonomiczną, zapisy oraz logistykę. Aby go wywołać, uruchom:
```bash
dotnet run -- --run-tests
```
Wszystkie testy powinny zakończyć się statusem `OK`.
