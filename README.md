# BeeNetFramework

基于[一个接口的实现](https://www.cnblogs.com/hkf7703/archive/2012/03/29/2423285.html) 想法， 设计的一套集成了ORM， MVC， API， swagger， jwt的一套快速开发平台。

    public interface IEntityProxy
    {

        /// <summary>

        /// 获取实例对象的属性值

        /// </summary>

        /// <param name="entity">对象实例</param>

        /// <param name="propertyName">属性名</param>

        /// <returns>属性值</returns>

        object GetPropertyValue(object entity, string propertyName);



        /// <summary>

        /// 设置实例对象的属性值

        /// </summary>

        /// <param name="entity">对象实例</param>

        /// <param name="propertyName">属性名</param>

        /// <param name="propertyValue">属性值</param>

        void SetPropertyValue(object entity, string propertyName, object propertyValue);



        /// <summary>

        /// 动态调用方法。

        /// </summary>

        /// <param name="entity">对象实例</param>

        /// <param name="methodName">方法名</param>

        /// <param name="dataAdapter">参数名，参数值数据集</param>

        /// <returns>返回值</returns>

        object Invoke(object entity, string methodName, BeeDataAdapter dataAdapter);
}

