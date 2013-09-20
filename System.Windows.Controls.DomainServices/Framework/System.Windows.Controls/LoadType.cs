using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace System.Windows.Controls
{
    /// <summary>
    /// Enum used to characterize the current load operation.
    /// </summary>
    internal enum LoadType
    {
        /// <summary>
        /// Used for non-existing deferred load
        /// </summary>
        None,

        /// <summary>
        /// Loading all items in a single shot
        /// </summary>
        LoadAll,

        /// <summary>
        /// Loading the first pages in a paging situation (occurs when PageSize becomes > 0)
        /// </summary>
        LoadFirstPages,

        /// <summary>
        /// Loading previous set of pages in a paging situation (occurs when PageIndex moves before current boundaries)
        /// </summary>
        LoadPreviousPages,

        /// <summary>
        /// Loading current set of pages in a paging situation (occurs with Refresh)
        /// </summary>
        LoadCurrentPages,

        /// <summary>
        /// Loading next set of pages in a paging situation (occurs when PageIndex moves beyond current boundaries)
        /// </summary>
        LoadNextPages,

        /// <summary>
        /// Loading last set of pages in a paging situation where PageIndex was set to a too large number
        /// </summary>
        LoadLastPages,

        /// <summary>
        /// Loading first chunk of items in a progressive download situation
        /// </summary>
        LoadFirstItems,

        /// <summary>
        /// Loading next chunk of items in a progressive download situation
        /// </summary>
        LoadNextItems
    }
}
