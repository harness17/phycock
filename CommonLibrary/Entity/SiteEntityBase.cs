namespace Dev.CommonLibrary.Entity
{
    /// <summary>
    /// サイトエンティティベース
    /// </summary>
    public abstract class PhycockEntityBase : EntityBase, IEntity
    {
        public long Id { get; set; }
    }
}
