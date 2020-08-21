using System.Threading.Tasks;

public static class TaskScheduler
{


    public static T Request<T>(System.Func<T> function)
    {
        Task<T> task = Task<T>.Factory.StartNew(() => function());

        return task.Result;
    }


}
