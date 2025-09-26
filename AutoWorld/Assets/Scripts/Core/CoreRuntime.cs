using System;

namespace AutoWorld.Core
{
    public static class CoreRuntime
    {
        private static GameSession session;

        public static GameSession Session
        {
            get
            {
                if (session == null)
                {
                    throw new InvalidOperationException("GameSession이 초기화되지 않았습니다.");
                }

                return session;
            }
        }

        public static void SetSession(GameSession value)
        {
            session = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static bool HasSession => session != null;
    }
}
