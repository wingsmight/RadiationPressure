public interface IShowable
{
    void Show();
}
public interface IShowable<T>
{
    void Show(T data);
}
