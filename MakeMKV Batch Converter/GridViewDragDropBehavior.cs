using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using Telerik.Windows.DragDrop;
using Telerik.Windows.DragDrop.Behaviors;

namespace MakeMKV_Batch_Converter
{
    public class GridViewDragDropBehavior
    {
        private RadGridView _associatedObject;
        /// <summary>
        /// AssociatedObject Property
        /// </summary>
        public RadGridView AssociatedObject
        {
            get
            {
                return _associatedObject;
            }
            set
            {
                _associatedObject = value;
            }
        }

        private static Dictionary<RadGridView, GridViewDragDropBehavior> instances;

        static GridViewDragDropBehavior()
        {
            instances = new Dictionary<RadGridView, GridViewDragDropBehavior>();
        }

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            GridViewDragDropBehavior behavior = GetAttachedBehavior(obj as RadGridView);

            behavior.AssociatedObject = obj as RadGridView;

            if (value)
            {
                behavior.Initialize();
            }
            else
            {
                behavior.CleanUp();
            }
            obj.SetValue(IsEnabledProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(GridViewDragDropBehavior),
                new PropertyMetadata(new PropertyChangedCallback(OnIsEnabledPropertyChanged)));

        public static void OnIsEnabledPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            SetIsEnabled(dependencyObject, (bool)e.NewValue);
        }

        private static GridViewDragDropBehavior GetAttachedBehavior(RadGridView gridview)
        {
            if (!instances.ContainsKey(gridview))
            {
                instances[gridview] = new GridViewDragDropBehavior();
                instances[gridview].AssociatedObject = gridview;
            }

            return instances[gridview];
        }

        protected virtual void Initialize()
        {
            this.UnsubscribeFromDragDropEvents();
            this.SubscribeToDragDropEvents();
        }

        protected virtual void CleanUp()
        {
            this.UnsubscribeFromDragDropEvents();
        }

        private void SubscribeToDragDropEvents()
        {
            DragDropManager.AddDragInitializeHandler(this.AssociatedObject, OnDragInitialize);
            DragDropManager.AddGiveFeedbackHandler(this.AssociatedObject, OnGiveFeedback);
            DragDropManager.AddDropHandler(this.AssociatedObject, OnDrop);
            DragDropManager.AddDragDropCompletedHandler(this.AssociatedObject, OnDragDropCompleted);
            DragDropManager.AddDragOverHandler(this.AssociatedObject, OnDragOver);
        }

        private void UnsubscribeFromDragDropEvents()
        {
            DragDropManager.RemoveDragInitializeHandler(this.AssociatedObject, OnDragInitialize);
            DragDropManager.RemoveGiveFeedbackHandler(this.AssociatedObject, OnGiveFeedback);
            DragDropManager.RemoveDropHandler(this.AssociatedObject, OnDrop);
            DragDropManager.RemoveDragDropCompletedHandler(this.AssociatedObject, OnDragDropCompleted);
            DragDropManager.RemoveDragOverHandler(this.AssociatedObject, OnDragOver);

        }

        private void OnDragInitialize(object sender, DragInitializeEventArgs e)
        {
            //DropIndicationDetails details = new DropIndicationDetails();
            var row = e.OriginalSource as GridViewRow ?? (e.OriginalSource as FrameworkElement).ParentOfType<GridViewRow>();

            var item = row != null ? row.Item : (sender as RadGridView).SelectedItem;
            //details.CurrentDraggedItem = item;

            IDragPayload dragPayload = DragDropPayloadManager.GeneratePayload(null);

            dragPayload.SetData("DraggedData", item);
            //dragPayload.SetData("DropDetails", details);

            e.Data = dragPayload;

            e.DragVisual = new DragVisual()
            {
                //Content = details,
                ContentTemplate = this.AssociatedObject.Resources["DraggedItemTemplate"] as DataTemplate
            };
            e.DragVisualOffset = e.RelativeStartPoint;
            e.AllowedEffects = DragDropEffects.All;
        }

        private void OnGiveFeedback(object sender, Telerik.Windows.DragDrop.GiveFeedbackEventArgs e)
        {
            e.SetCursor(Cursors.Arrow);
            e.Handled = true;
        }

        private void OnDragDropCompleted(object sender, DragDropCompletedEventArgs e)
        {
            var draggedItem = DragDropPayloadManager.GetDataFromObject(e.Data, "DraggedData");

            if (e.Effects != DragDropEffects.None)
            {
                var collection = (sender as RadGridView).ItemsSource as IList;
                collection.Remove(draggedItem);
            }
        }

        private void OnDrop(object sender, Telerik.Windows.DragDrop.DragEventArgs e)
        {
            var draggedItem = DragDropPayloadManager.GetDataFromObject(e.Data, "DraggedData");
            var itemsType = (this.AssociatedObject.ItemsSource as IList).AsQueryable().ElementType;
            //var details = DragDropPayloadManager.GetDataFromObject(e.Data, "DropDetails") as DropIndicationDetails;

            //if (details == null || draggedItem == null || draggedItem.GetType() != itemsType)
            {
               // return;
            }

            if (e.Effects != DragDropEffects.None)
            {
                var collection = (sender as RadGridView).ItemsSource as IList;
                collection.Add(draggedItem);
            }

            e.Handled = true;
        }

        private void OnDragOver(object sender, Telerik.Windows.DragDrop.DragEventArgs e)
        {
            var draggedItem = DragDropPayloadManager.GetDataFromObject(e.Data, "DraggedData");
            var itemsType = (this.AssociatedObject.ItemsSource as IList).AsQueryable().ElementType;


            if (draggedItem.GetType() != itemsType)
            {
                e.Effects = DragDropEffects.None;
            }

            //var dropDetails = DragDropPayloadManager.GetDataFromObject(e.Data, "DropDetails") as DropIndicationDetails;
            //dropDetails.CurrentDraggedOverItem = this.AssociatedObject;
            //dropDetails.CurrentDropPosition = Controls.DropPosition.Inside;

            e.Handled = true;
        }
    }
}
