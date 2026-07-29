namespace BS.GamePlay.Run
{
    /// <summary>
    /// 纯计时器，只负责记录时间流逝，不涉及游戏逻辑判断。
    /// </summary>
    public class RunTimer
    {
        private float duration;
        private float elapsed;

        public RunTimer(float duration)
        {
            this.duration = duration;
            elapsed = 0f;
        }

        /// <summary>
        /// 总计时长（秒）
        /// </summary>
        public float Duration => duration;

        /// <summary>
        /// 已流逝时间（秒）
        /// </summary>
        public float Elapsed => elapsed;

        public float Remaining
        {
            get
            {
                float remaining = duration - elapsed;
                return remaining < 0f ? 0f : remaining;
            }
        }
        /// <summary>
        /// 归一化进度 [0, 1]
        /// </summary>
        public float Normalized
        {
            get
            {
                if (duration <= 0f)
                    return 1f; // 防止除零，认为已满

                float normalized = elapsed / duration;
                return normalized < 0f ? 0f : (normalized > 1f ? 1f : normalized);
            }
        }
        /// <summary>
        /// 是否已到达或超过总时长
        /// </summary>
        public bool IsFinished => elapsed >= duration;

        /// <summary>
        /// 推进时间（由外部每帧调用）
        /// </summary>
        /// <param name="deltaTime">增量时间（秒）</param>
        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
                return; // 不处理负增量

            elapsed += deltaTime;

            // 不主动截断，让 Remaining 和 Normalized 自行处理边界
        }
        /// <summary>
        /// 重置计时器（将已流逝时间归零）
        /// </summary>
        public void Reset()
        {
            elapsed = 0f;
        }
    }
}
