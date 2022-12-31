namespace WillsWackyManagers.UnityTools
{
    public interface ISaveableCard
    {
        /// <summary>
        /// Needs to be linked up to a static field in that card type, or some other method of saving the card.
        /// </summary>
        CardInfo Card { get; set; }
    }
}
