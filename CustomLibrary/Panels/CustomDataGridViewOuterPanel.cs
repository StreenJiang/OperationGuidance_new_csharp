namespace CustomLibrary.Panels {
    public abstract class CustomDataGridViewOuterPanel<DTO, VO>: CustomContentPanel {
        protected abstract List<VO> QueryList();
        protected abstract void AddOrUpdate(DTO dto, Action action);
        protected abstract void Delete(List<int> ids);
    }
}
