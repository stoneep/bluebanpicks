namespace Common.Pooling
{
    public interface IUIReusable
    {
        void OnRent();
        void OnReturn();
    }
}