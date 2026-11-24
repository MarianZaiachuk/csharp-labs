# Лабораторна робота №2. Масиви та алгоритм гри «Хрестики-нулики»

## Мета роботи
Навчитися представляти ігрове поле розміром $N \times N$ двовимірним масивом `CellState[,]`, відображати його засобами WPF (`ItemsControl` + `UniformGrid`) та будувати алгоритм перевірки перемоги й вибору ходу комп'ютера циклами без дублювання однотипних умов.

## Представлення поля та алгоритми
- Ігрове поле задається двовимірним масивом `CellState[BoardSize, BoardSize]`, де базовий розмір `DefaultBoardSize = 3` визначено константою.
- Перевірка перемоги виконується методом `CheckWinner()` через єдину допоміжну процедуру `CheckLine(startRow, startCol, dRow, dCol, out line)`: циклічно перевіряються всі $N$ рядків, $N$ стовпців і дві діагоналі без жорстко прописаних перевірок трійок через `if`. Часова складність перевірки перемоги на полі $N \times N$ становить $O(N^2)$.
- Реалізовано три рівні складності комп'ютерного суперника (`DifficultyLevel`):
  1. **Рівень 1 (`Random`)** — довільний коректний хід зі списку `GetAvailableMoves()`.
  2. **Рівень 2 (`Block`)** — блокування негайного виграшу людини, інакше випадковий вільний хід.
  3. **Рівень 3 (`WinAndBlock`)** — спочатку пошук власного виграшного ходу, потім блокування суперника, інакше довільний вільний хід.

## Архітектура проєкту
- `TicTacToeApp/Models/` — чиста модель гри (`TicTacToeGame`, `CellState`, `DifficultyLevel`, `GameStatus`, `BoardPosition`) без залежності від WPF (`System.Windows`). Окремі методи: `MakeMove`, `GetAvailableMoves`, `CheckWinner`, `IsDraw`, `ChooseComputerMove`.
- `TicTacToeApp/ViewModels/` — `MainViewModel`, `CellViewModel`, `DifficultyOption` (`INotifyPropertyChanged`, `ObservableCollection<CellViewModel>`).
- `TicTacToeApp/Commands/` — `RelayCommand` (`ICommand`).
- `TicTacToeApp/MainWindow.xaml` — інтерфейс на `DockPanel`, `Grid`, `UniformGrid` та `ItemsControl`. У `MainWindow.xaml.cs` ігрова логіка відсутня.
- `TicTacToeApp.Tests/` — модульні тести (`xUnit`) для перевірки виграшу, нічиєї та логіки комп'ютера.

## Контрольні приклади
| Сценарій | Опис | Очікуваний результат |
|---|---|---|
| Перемога по рядку / стовпцю / діагоналі | Гравець (X) або комп'ютер (O) заповнює суцільну лінію з 3 однакових символів | `CheckWinner()` повертає переможця, клітинки лінії підсвічуються зеленим, інші клітинки стають недоступними |
| Нічия (граничний випадок) | Заповнені всі 9 клітинок без виграшної лінії | `IsDraw()` повертає `true`, статус `"Нічия!"`, рахунок нічиїх збільшується |
| Рівень 3: власна перемога vs блокування | На полі одночасно є виграшний хід для O і загроза виграшу X | `ChooseComputerMove(DifficultyLevel.WinAndBlock)` обирає власний виграшний хід раніше за блокування |
| Помилковий / повторний хід | Спроба ходу в зайняту клітинку, за межі поля або після завершення партії | `MakeMove(...)` повертає `false`, стан поля не змінюється, аварійне завершення виключене |

## Запуск і тестування
```powershell
dotnet build Lab02_TicTacToe/Lab02_TicTacToe.sln
dotnet test Lab02_TicTacToe/Lab02_TicTacToe.sln
dotnet run --project Lab02_TicTacToe/TicTacToeApp/TicTacToeApp.csproj
```
