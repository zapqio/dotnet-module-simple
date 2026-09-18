# module-simple

Przykładowy moduł runnera Zapqio, zbudowany na kontrakcie
[`Zapqio.Runner.Module.Core`](https://github.com/zapqio/runner-dotnet) 1.2 (runner 0.1.11 lub
nowszy). Trzy metody:

- **Welcome Jon** - wejście `{ "Count": n }`, zwraca listę powitań. Pokazuje `IRunnerMethod`,
  wstrzykiwanie własnej usługi przez `IRunnerInjection` (`MyName`), logi z `Console`, które
  trafiają na żywo do historii zadania w panelu, i `RunnerLog` tam, gdzie konsola nie wystarcza:
  `Debug` z wejściem i `Warning`, gdy `Count` jest zerowe.
- **Wait** - wejście `{ "Seconds": n }` (domyślnie 5), śpi tyle sekund i oddaje kontekst zadania
  (`JobId`, `AttemptId`). Służy do oglądania, co platforma robi, gdy runner zniknie w trakcie
  kroku: wynik nieznany, ponowienie przez operatora, wynik dosłany po powrocie runnera.
- **Log levels** - wejście `{ "Text": "...", "WithException": false }`. Pisze jeden wpis na każdy
  poziom każdym z trzech kanałów (`RunnerLog`, wstrzyknięty `ILogger<T>`, konsola), do tego wpis z
  wątku `Task.Run` i opcjonalnie wpis z wyjątkiem. Zwraca, które poziomy runner przy swoim progu w
  ogóle wysyła do panelu - do porównania z tym, co w historii zadania faktycznie widać.

## Logowanie z poziomem (`Module.Core` 1.2)

Konsola daje tylko dwa poziomy: `Console.WriteLine` to `Info`, `Console.Error` to `Error`. Kto chce
nazwać wagę wpisu wprost, ma dwa sposoby, oba kończące w tym samym miejscu:

| Sposób | Jak | Kiedy |
| --- | --- | --- |
| `RunnerLog.Debug/Info/Warning/Error/Critical(...)` | klasa statyczna z `Zapqio.Runner.Core`, bez niczego w konstruktorze | metody pomocnicze, klasy statyczne, wątki uruchomione przez metodę |
| `ILogger<T>` w konstruktorze | runner rejestruje go w kontenerze modułów; `Trace` schodzi do `Debug` | kto woli wstrzykiwanie i szablony `{Nazwa}` |

Wpis trafia do zadania, w którego kontekście powstał (`JobContext.Current`), także z `Task.Run`.
`Debug` domyślnie **nie** idzie do panelu - runner wysyła od poziomu `Info` (ustawienie
`MinRemoteLogLevel` / `ZAPQIO_MIN_LOG_LEVEL`); wpisy poniżej progu zostają w logu plikowym
runnera. Przed kosztownym budowaniem treści warto zapytać `RunnerLog.IsEnabled(RunnerLogLevel.Debug)`.
`RunnerLog.Error(ex, "...")` dopisuje treść wyjątku pod wiadomością, w jednym wpisie.

Paczka `Microsoft.Extensions.Logging.Abstractions` jest w csproj z `ExcludeAssets="runtime"`: moduł
kompiluje się przeciw niej, ale w zipie jej nie ma, bo runner ma własną kopię i to jej typami
rejestruje logger. Druga kopia w paczce rozjechałaby typy i `ILogger<T>` nie dałby się wstrzyknąć.

## Kontekst zadania (`Module.Core` 1.1)

W `Run` moduł może odczytać `JobContext.Current`:

| Pole | Znaczenie |
| --- | --- |
| `JobId` | identyfikator **operacji** - ten sam przy każdej wysyłce i każdym ponowieniu tego zadania |
| `AttemptId` | identyfikator tej wysyłki - inny za każdym razem |
| `MethodName` | nazwa metody z przydziału |

Platforma wysyła to samo zadanie drugi raz, gdy straciła runnera po starcie metody i nie wie, jak
się skończyła, albo gdy operator świadomie ponowił krok. Metoda z nieodwracalnym skutkiem (faktura,
mail, przelew) powinna zapisać `JobId` razem ze skutkiem i przed wykonaniem sprawdzić, czy skutek z
tym identyfikatorem już istnieje - wtedy oddaje go zamiast tworzyć drugi. `Wait` tylko wypisuje
kontekst; wzorzec jest w komentarzu w `Wait.cs`.

Poza `Run` (konstruktor, `NameMethod`) kontekst jest pusty. Moduły zbudowane pod `Module.Core` 1.0
działają bez zmian - wersja zestawu jest ta sama, doszedł tylko nowy typ.

## Budowanie

```
dotnet publish -c Release
```

Po publikacji obok katalogu `publish` powstaje `Simple.zip` - wrzuć go do katalogu `Modules`
runnera i zrestartuj usługę.
