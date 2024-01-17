namespace CustomLibrary.Panels {
    public abstract class CustomDataGridViewOuterPanel<T>: CustomContentPanel {
        protected abstract List<T> QueryList();
        protected abstract void Add(T entity);
        protected abstract void Update(T entity);
        protected abstract void Delete(List<int> ids);
    }
}
