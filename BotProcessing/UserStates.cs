namespace BotProcessing;

public class UserStates
{
    // С помощью enum будем отслеживать на какой стадии работы программы находится пользователь.
    // Это нужно для работы с несколькими пользователями так как в таком случае нельзя отслеживать
    // что пользователь делает корректные действия с помощью bool переменных
    // (например пользователь может написать боту что хочет произвести сортировку, не загрузив файл)
    public enum UserState
    {
        Default,
        GotFile,
        FilterByDistrict,
        FilterByAdmAreaAndOwner,
        FilterByOwner,
        SortingSelection,
        FilterSelection,
        WaitForCsv,
        WaitForJson,
    }
}
