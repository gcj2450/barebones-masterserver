using System;
using System.Collections.Generic;

namespace Barebones.MasterServer
{
    /// <summary>
    /// 服务器模块接口
    /// </summary>
    public interface IServerModule
    {
        IEnumerable<Type> Dependencies { get; }
        IEnumerable<Type> OptionalDependencies { get; }

        /// <summary>
        /// Server, which initialized this module.
        /// Will be null, until the module is initialized
        /// </summary>
        ServerBehaviour Server { get; set; }

        void Initialize(IServer server);
    }
}