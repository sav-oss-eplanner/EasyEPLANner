namespace EasyEPlanner.Binding.ViewModel
{
    public static class BindingSearch
    {
        public static bool Contains(string valueForSearch, string searchedValue) =>
            Editor.Search.Contains(valueForSearch, searchedValue);
    }
}
