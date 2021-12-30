using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIME
{
    #region ラッパー列挙子 IEnumeratorPlus<T>
    ///// <summary>
    ///// MoveNext()の返信値を IsAliveプロパティとして保持できるラッパー列挙子
    ///// </summary>
    ///// <typeparam name="T"></typeparam>
    //public class IEnumeratorPlus<T> : IEnumerator<T>
    //{
    //    private IEnumerator<T> _sorce;
    //    public int Index { get; private set; } = -1;
    //    public bool IsAlive { get; private set; }
    //    public IEnumeratorPlus(IEnumerator<T> sorce)
    //    {
    //        _sorce = sorce;
    //    }

    //    public T Current => _sorce.Current;

    //    object System.Collections.IEnumerator.Current => ((System.Collections.IEnumerator)_sorce).Current;

    //    public void Dispose()
    //    {
    //        _sorce.Dispose();
    //    }

    //    public bool MoveNext()
    //    {
    //        var result = _sorce.MoveNext();
    //        if (result)
    //        {
    //            Index++;
    //        }
    //        IsAlive = result;
    //        return result;
    //    }

    //    public void Reset()
    //    {
    //        _sorce.Reset();
    //    }
    //}
    #endregion

}
