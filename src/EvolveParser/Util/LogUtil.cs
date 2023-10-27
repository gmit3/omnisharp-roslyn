namespace EvolveUI {

    public static class LogUtil {

        public static void Log(string message) {
#if UNITY_64
            Debug.Log(message);
#else
            Console.WriteLine(message);
#endif
        }

        public static void Error(string message) {
#if UNITY_64
            Debug.LogError(message);
#else
            Console.Error.WriteLine(message);
#endif
        }

    }

}