// leading namespace comment
namespace Text.Namespace1; // namespace comment

public class Class1
{
    #region test1

    public bool Region1 => true;
    public bool _region1 = true;

    #endregion


    ///<summary>
    /// test1 summary
    ///</summary>
    public bool _test1 = true;
    private bool _test2 = true;
    private bool _test3 = true; // test3 comment
}
