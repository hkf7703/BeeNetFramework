using System;
using System.Collections.Generic;
using System.Linq;
using Ex = System.Linq.Expressions.Expression;
using System.Text;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using Bee.Core;
using System.Collections;
using Bee.Data;

namespace Bee.Collections
{
    /*
    public class IndexableCollection<T> : Collection<T>
        where T : class
    {

        //this defines a dictionary of dictionaries of lists of some type we are being a collection of :)
        //the index is always the hash of whatever we are indexing.
        private Dictionary<string, Dictionary<int, List<T>>> _indexes = new Dictionary<string, Dictionary<int, List<T>>>();

        private EntityProxy<T> entityProxy;

        public IndexableCollection() :
            this(new List<T>())
        {

        }

        public IndexableCollection(IEnumerable<T> items)
            : this(items, new IndexSpecification<T>())
        {

        }

        public IndexableCollection(IndexSpecification<T> indexSpecification)
            : this(new List<T>(), indexSpecification)
        {
            entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();
        }



        public IndexableCollection(IEnumerable<T> items, IndexSpecification<T> indexSpecification)
        {
            if (indexSpecification == null)
                throw new ArgumentNullException("indexSpecification");

            //TODO: should we validate items argument for null?

            UseIndexSpecification(indexSpecification);

            foreach (var item in items)
                this.Add(item);
        }

        public IndexableCollection<T> CreateIndexFor<TParameter>(Expression<Func<T, TParameter>> propertyExpression)
        {
            var propertyName = propertyExpression.GetMemberName();

            return CreateIndexFor(propertyName); ;
        }

        public bool RemoveIndexFor<TParameter>(Expression<Func<T, TParameter>> propertyExpression)
        {
            var propertyName = propertyExpression.GetMemberName();

            if (_indexes.ContainsKey(propertyName))
                return _indexes.Remove(propertyName);

            return false;
        }

        public bool ContainsIndex<TParameter>(Expression<Func<T, TParameter>> propertyExpression)
        {
            return ContainsIndex(propertyExpression.GetMemberName());
        }

        public bool ContainsIndex(string propertyName)
        {
            return _indexes.ContainsKey(propertyName);
        }

        public Dictionary<int, List<T>> GetIndexByPropertyName(string propName)
        {
            return _indexes[propName];
        }

        public new void Add(T item)
        {
            foreach (string key in _indexes.Keys)
            {
                AddToIndex(key, item, _indexes[key]);
            }

            base.Add(item);
        }

        public new bool Remove(T item)
        {
            foreach (string key in _indexes.Keys)
            {
                var itemValue = entityProxy.GetPropertyValue(item, key);
                RemoveItem(item, key, itemValue);
            }
            return base.Remove(item);
        }

        public IndexableCollection<T> UseIndexSpecification(IndexSpecification<T> indexSpecification)
        {
            if (indexSpecification == null)
                throw new ArgumentNullException("indexSpecification");

            foreach (IndexMeta indexMeta in indexSpecification.IndexedProperties)
            {
                this.CreateIndexFor(indexMeta);
            }

            return this;
        }


        //TODO:
        // what about instead of 
        //      foreach index
        //          foreach item
        //              build up index
        //
        // we flip it and try something like
        //      foreach item
        //          foreach index
        //              build up index
        //
        // TOOD: just prototype & speed test the scenerio.
        // I have a feeling it won't be faster, in fact may be slower due to the creation of all the extra iterators
        //public IndexableCollection<T> UseIndexSpecificationX(IndexSpecification<T> indexSpec)
        //{
        //    IndexableCollection<T> oldIndex = this;

        //    IndexableCollection<T> newIndex = new IndexableCollection<T>();
        //    newIndex.UseIndexSpecification(indexSpec);

        //    for (int i = 0; i < oldIndex.Count; i++)
        //    {
        //        newIndex.Add(oldIndex[i]);
        //    }

        //    return newIndex;
        //}

        private IndexableCollection<T> CreateIndexFor(IndexMeta indexMeta)
        {
            var newIndex = new Dictionary<int, List<T>>();

            for (int i = 0; i < this.Count; i++)
            {
                AddToIndex(propertyName, this[i], newIndex);
            }

            _indexes.Add(propertyName, newIndex);

            return this;
        }


        private void AddToIndex(string propertyName, T newItem, Dictionary<int, List<T>> index)
        {
            var propertyValue = entityProxy.GetPropertyValue(newItem, propertyName);
            if (propertyValue != null)
            {
                AddValueToIndex(newItem, index, propertyValue);
            }
        }

        private static void AddValueToIndex(T newItem, Dictionary<int, List<T>> index, object propertyValue)
        {
            int hashCode = propertyValue.GetHashCode();
            List<T> list;

            if (index.TryGetValue(hashCode, out list))
                list.Add(newItem);
            else
                index.Add(hashCode, new List<T> { newItem });
        }


        private void RemoveItem(T item, string key, object itemValue)
        {
            int hashCode = itemValue.GetHashCode();
            Dictionary<int, List<T>> index = _indexes[key];
            if (index.ContainsKey(hashCode))
                index[hashCode].Remove(item);

            //new IndexSpecification<BeeDataAdapter>().Add(
        }

    }
     * 
     */

    public enum IndexType
    {
        Value,
        Hash,
        Token
    }

    public class IndexMeta
    {
        public string PropertyName { get; set; }
        public IndexType IndexType { get; set; }
        public Type PropertyType { get; set; }
    }


    public class IndexValuePair<T1, T2>
    {
        public T1 IndexValue { get; set; }
        public T2 Value { get; set; }

        public override int GetHashCode()
        {
            return IndexValue.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            IndexValuePair<T1, T2> item = obj as IndexValuePair<T1, T2>;
            return item != null && item.IndexValue.Equals(this.IndexValue);
        }
    }

    public class IntIndexValue<T> : IndexValuePair<int, T>
    {
        public static int Comparison(IntIndexValue<T> x, IntIndexValue<T> y)
        {
            return x.IndexValue.CompareTo(y.IndexValue);
        }
    }

    public class LongIndexValue<T> : IndexValuePair<long, T>
    {
        public static int Comparison(LongIndexValue<T> x, LongIndexValue<T> y)
        {
            return x.IndexValue.CompareTo(y.IndexValue);
        }
    }

    public class StringIndexValue<T> : IndexValuePair<string, T>
    {
        public static int Comparison(StringIndexValue<T> x, StringIndexValue<T> y)
        {
            return x.IndexValue.CompareTo(y.IndexValue);
        }
    }

    public class IndexList<T>
        where T : class
    {
        private IndexMeta indexMeta;
        private EntityProxy<T> proxy;
        private List<IntIndexValue<T>> intIndexList = new List<IntIndexValue<T>>();
        private List<LongIndexValue<T>> longIndexList = new List<LongIndexValue<T>>();
        private List<StringIndexValue<T>> stringIndexList = new List<StringIndexValue<T>>();

        public IndexList(IndexMeta indexMeta, List<T> list)
        {
            this.indexMeta = indexMeta;
            proxy = EntityProxyManager.Instance.GetEntityProxy<T>();

            CreateIndexValue(list);
        }

        private IList GetIndexList()
        {
            IList result = null;

            TypeCode typeCode = Type.GetTypeCode(indexMeta.PropertyType);
            switch(typeCode)
            {
                case TypeCode.Int32:
                    result = intIndexList;
                    break;
                case TypeCode.Int64:
                case TypeCode.DateTime:
                    result = longIndexList;
                    break;
                case TypeCode.String:
                    result = stringIndexList;
                    break;
            }

            return result;
        }

        private void CreateIndexValue(List<T> list)
        {
            IList indexList = GetIndexList();
            if (indexList == null) return;
            TypeCode typeCode = Type.GetTypeCode(indexMeta.PropertyType);
            foreach (T item in list)
            {
                switch (typeCode)
                {
                    case TypeCode.Int32:
                        indexList.Add(new IntIndexValue<T>() {
                            IndexValue = (int)(proxy.GetPropertyValue(item, indexMeta.PropertyName))
                            , Value = item });
                        break;
                    case TypeCode.Int64:
                        indexList.Add(new LongIndexValue<T>()
                        {
                            IndexValue = (long)(proxy.GetPropertyValue(item, indexMeta.PropertyName)),
                            Value = item
                        });
                        break;
                    case TypeCode.DateTime:
                        indexList.Add(new LongIndexValue<T>()
                        {
                            IndexValue = ((DateTime)(proxy.GetPropertyValue(item, indexMeta.PropertyName))).Ticks,
                            Value = item
                        });
                        break;
                    case TypeCode.String:
                        indexList.Add(new StringIndexValue<T>()
                        {
                            IndexValue = (string)(proxy.GetPropertyValue(item, indexMeta.PropertyName)),
                            Value = item
                        });
                        break;
                }
            }

            switch (typeCode)
            {
                case TypeCode.Int32:
                    List<IntIndexValue<T>> intIndexList = indexList as List<IntIndexValue<T>>;
                    intIndexList.Sort(IntIndexValue<T>.Comparison);
                    break;
                case TypeCode.Int64:
                case TypeCode.DateTime:
                    List<LongIndexValue<T>> longIndexList = indexList as List<LongIndexValue<T>>;
                    longIndexList.Sort(LongIndexValue<T>.Comparison);
                    break;
                case TypeCode.String:
                    List<StringIndexValue<T>> stringIndexList = indexList as List<StringIndexValue<T>>;
                    stringIndexList.Sort(StringIndexValue<T>.Comparison);
                    break;
            }
        }

        internal List<IntIndexValue<T>> Search(Criterion criteria)
        {
            List<IntIndexValue<T>> result = new List<IntIndexValue<T>>();
            if (criteria.CriterionType == CriterionType.Equal)
            {
                IList indexList = GetIndexList();
                if (indexList != null)
                {
                    //switch (typeCode)
                    //{
                    //    case TypeCode.Int32:
                    //        List<IntIndexValue<T>> intIndexList = indexList as List<IntIndexValue<T>>;
                    //        //intIndexList.BinarySearch(
                    //        //break;
                    //}
                }
            }
            return null;
        }
    }

    public static class IndexFactory
    {
        //public List<object>
    }

    public class IndexSpecification<T>
        where T : class
    {
        public List<IndexMeta> IndexedProperties { get; private set; }

        public IndexSpecification()
        {
            IndexedProperties = new List<IndexMeta>();
        }

        public IndexSpecification<T> Add(string propertyName)
        {
            return Add(propertyName, IndexType.Value);
        }

        public IndexSpecification<T> Add(string propertyName, IndexType indexType)
        {
            EntityProxy<T> proxy = EntityProxyManager.Instance.GetEntityProxy<T>();
            PropertySchema schema = proxy.GetProperty(propertyName);
            if (schema == null)
            {
                return this;
            }

            bool containFlag = false;
            foreach (IndexMeta item in IndexedProperties)
            {
                if (string.Compare(item.PropertyName, schema.Name, false) == 0)
                {
                    containFlag = true;
                    break;
                }
            }

            if (!containFlag)
            {
                IndexedProperties.Add(new IndexMeta() {PropertyName=schema.Name, IndexType=indexType, PropertyType=schema.PropertyType});
            }

            return this;
        }

        public IndexSpecification<T> Remove(string propertyName)
        {
            foreach (IndexMeta item in IndexedProperties)
            {
                if (string.Compare(item.PropertyName, propertyName, false) == 0)
                {
                    IndexedProperties.Remove(item);
                    break;
                }
            }

            return this;
        }
    }

    /*
    public static class IndexableCollectionExtension
    {
        public static IndexableCollection<T> ToIndexableCollection<T>(this IEnumerable<T> enumerable)
            where T : class
        {
            return new IndexableCollection<T>(enumerable);
        }

        public static IndexableCollection<T> ToIndexableCollection<T>(this IEnumerable<T> enumerable, IndexSpecification<T> indexSpecification)
            where T : class
        {
            return new IndexableCollection<T>(enumerable)
                .UseIndexSpecification(indexSpecification);
        }

        public static string GetMemberName<T, TProperty>(this Expression<Func<T, TProperty>> propertyExpression)
        {
            return ((MemberExpression)(((LambdaExpression)(propertyExpression)).Body)).Member.Name;
        }

        private static int? GetHashRight(Expression leftSide, Expression rightSide)
        {
            if (leftSide.NodeType == ExpressionType.Call)
            {
                var call = leftSide as System.Linq.Expressions.MethodCallExpression;
                if (call.Method.Name == "CompareString")
                {
                    LambdaExpression evalRight = Ex.Lambda(call.Arguments[1], null);
                    //Compile it, invoke it, and get the resulting hash
                    return (evalRight.Compile().DynamicInvoke(null).GetHashCode());
                }
            }
            //rightside is where we get our hash...
            switch (rightSide.NodeType)
            {
                //shortcut constants, dont eval, will be faster
                case ExpressionType.Constant:
                    ConstantExpression constExp
                      = (ConstantExpression)rightSide;
                    return (constExp.Value.GetHashCode());

                //if not constant (which is provably terminal in a tree), convert back to Lambda and eval to get the hash.
                default:
                    //Lambdas can be created from expressions... yay
                    LambdaExpression evalRight = Ex.Lambda(rightSide, null);
                    //Compile that mutherf-ker, invoke it, and get the resulting hash
                    return (evalRight.Compile().DynamicInvoke(null).GetHashCode());
            }
        }

        private static bool HasIndexablePropertyOnLeft<T>(Expression leftSide, IndexableCollection<T> sourceCollection, out MemberExpression theMember)
            where T : class
        {
            theMember = null;
            MemberExpression mex = leftSide as MemberExpression;
            if (leftSide.NodeType == ExpressionType.Call)
            {
                var call = leftSide as System.Linq.Expressions.MethodCallExpression;
                if (call.Method.Name == "CompareString")
                {
                    mex = call.Arguments[0] as MemberExpression;
                }
            }

            if (mex == null) return false;

            theMember = mex;
            return sourceCollection.ContainsIndex(((MemberExpression)mex).Member.Name);

        }

        //extend the where when we are working with indexable collections! 
        public static IEnumerable<T> Where<T>(this IndexableCollection<T> sourceCollection, Expression<Func<T, bool>> expr)
            where T : class
        {
            //our indexes work from the hash values of that which is indexed, regardless of type
            int? hashRight = null;
            bool noIndex = true;
            //indexes only work on equality expressions here
            if (expr.Body.NodeType == ExpressionType.Equal)
            {
                //Equality is a binary expression
                BinaryExpression binExp = (BinaryExpression)expr.Body;
                //Get some aliases for either side
                Expression leftSide = binExp.Left;
                Expression rightSide = binExp.Right;

                hashRight = GetHashRight(leftSide, rightSide);

                //if we were able to create a hash from the right side (likely)
                MemberExpression returnedEx = null;
                if (hashRight.HasValue && HasIndexablePropertyOnLeft<T>(leftSide, sourceCollection, out returnedEx))
                {
                    //cast to MemberExpression - it allows us to get the property
                    MemberExpression propExp = (MemberExpression)returnedEx;
                    string property = propExp.Member.Name;
                    Dictionary<int, List<T>> myIndex =
                      sourceCollection.GetIndexByPropertyName(property);
                    if (myIndex.ContainsKey(hashRight.Value))
                    {
                        IEnumerable<T> sourceEnum = myIndex[hashRight.Value].AsEnumerable<T>();
                        IEnumerable<T> result = sourceEnum.Where<T>(expr.Compile());
                        foreach (T item in result)
                            yield return item;
                    }
                    noIndex = false; //we found an index, whether it had values or not is another matter
                }

            }
            if (noIndex) //no index?  just do it the normal slow way then...
            {
                IEnumerable<T> sourceEnum = sourceCollection.AsEnumerable<T>();
                IEnumerable<T> result = sourceEnum.Where<T>(expr.Compile());
                foreach (T resultItem in result)
                    yield return resultItem;
            }

        }
    }
     * 
     */ 
}
