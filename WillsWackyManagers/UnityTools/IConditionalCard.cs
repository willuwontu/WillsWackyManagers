namespace WillsWackyManagers.UnityTools
{
    public interface IConditionalCard : ISaveableCard
    {
        /// <summary>
        /// Should be linked with the <see cref="ISaveableCard.Card"/> field for determining if a card is allowed.
        /// </summary>
        bool Condition(Player player, CardInfo card);
    }
}
