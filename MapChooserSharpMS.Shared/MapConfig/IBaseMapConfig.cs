namespace MapChooserSharpMS.Shared.MapConfig;

public interface IBaseMapConfig
{
    /// <summary>
    /// Is this config data disabled?
    /// </summary>
    public bool IsDisabled { get; }
    
    /// <summary>
    /// How many extends available in this map?
    /// </summary>
    public int MaxExtends { get; }
    
    /// <summary>
    /// How many times allow the !ext command in this map?
    /// </summary>
    public int MaxExtCommandUses { get; }
    
    /// <summary>
    /// Map's default mp_timelimit value
    /// </summary>
    public int MapTime { get; }
    
    /// <summary>
    /// How many minutes extended in per extend?
    /// </summary>
    public int ExtendTimePerExtends { get; }
    
    /// <summary>
    /// Map's default mp_maxround
    /// </summary>
    public int MapRounds { get; }
    
    /// <summary>
    /// How many rounds extended in per extend?
    /// </summary>
    public int ExtendRoundsPerExtends { get; }
    
    /// <summary>
    /// Random pick settings
    /// </summary>
    public IRandomPickConfig RandomPickConfig { get; }
    
    /// <summary>
    /// Nomination settings
    /// </summary>
    public INominationConfig NominationConfig { get; }
    
    /// <summary>
    /// Cooldown things
    /// </summary>
    public ICooldownConfig CooldownConfig { get; }
    
    /// <summary>
    /// Type-safe accessor for extra configuration defined by API developers. <br/>
    /// <br/>
    /// If defined in config like this: <br/>
    /// [ze_example.extra.shop] <br/>
    /// cost = 100 <br/>
    /// <br/>
    /// Then you can access the value like this: <br/>
    /// int cost = ExtraConfiguration.GetValue&lt;int&gt;("shop", "cost", 0);
    /// </summary>
    public IExtraConfigAccessor ExtraConfiguration { get; }
}