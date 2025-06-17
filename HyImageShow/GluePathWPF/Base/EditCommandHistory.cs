using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization.Formatters;

namespace HyImageShow
{
    public class EditCommandHistory : INotifyPropertyChanged
    {
        public EditCommandHistory()
        {

        }

        private bool CanRedo => RedoCommands.Count > 0;
        private bool CanUndo => UndoCommands.Count > 0;

        public Stack<IEditCommand> RedoCommands { get; } = new Stack<IEditCommand>();
        public Stack<IEditCommand> UndoCommands { get; } = new Stack<IEditCommand>();

        private Stack<string> redoCommandsString { get; } = new Stack<string>();
        private Stack<string> undoCommandsString { get; } = new Stack<string>();

        public List<string> RedoCommandsString
        {
            get
            {
                List<string> redoCommands = new List<string>();

                foreach (var redo in redoCommandsString.Reverse().ToList())
                {
                    redoCommands.Add(redo);
                }

                return redoCommands;
            }
        }

        public List<string> UndoCommandsString
        {
            get
            {
                List<string> undoCommands = new List<string>();

                foreach (var undo in undoCommandsString.Reverse().ToList())
                {
                    undoCommands.Add(undo);
                }

                return undoCommands;
            }
        }

        public void ClearAll()
        {
            RedoCommands.Clear();
            UndoCommands.Clear();
            redoCommandsString.Clear();
            undoCommandsString.Clear();
        }

        private void AddRedoHistory(IEditCommand editCommand)
        {
            if (GluePathEditorViewModel.IsLoadingFile)
                return;

            RedoCommands.Push(editCommand);
            redoCommandsString.Push(editCommand.Name);

            OnPropertyChanged(nameof(RedoCommands));
            OnPropertyChanged(nameof(RedoCommandsString));
        }

        private void AddRedoHistoryInternal(IEditCommand editCommand)
        {
            if (GluePathEditorViewModel.IsLoadingFile)
                return;

            RedoCommands.Push(editCommand);
            redoCommandsString.Push("復原 " + editCommand.Name);

            OnPropertyChanged(nameof(RedoCommands));
            OnPropertyChanged(nameof(RedoCommandsString));
        }


        public void AddUndoHistory(IEditCommand editCommand, bool needClearRedo)
        {
            if (GluePathEditorViewModel.IsLoadingFile)
                return;


            UndoCommands.Push(editCommand);
            undoCommandsString.Push(editCommand.Name);
        
            OnPropertyChanged(nameof(UndoCommands));
            OnPropertyChanged(nameof(UndoCommandsString));

            if (needClearRedo)
            {
                RedoCommands.Clear();
                redoCommandsString.Clear();
                OnPropertyChanged(nameof(RedoCommands));
                OnPropertyChanged(nameof(redoCommandsString));
            }
        }

        public void AddUndoHistoryInternal(IEditCommand editCommand)
        {
            if (GluePathEditorViewModel.IsLoadingFile)
                return;

            UndoCommands.Push(editCommand);
            undoCommandsString.Push("重做" + editCommand.Name);

            OnPropertyChanged(nameof(UndoCommands));
            OnPropertyChanged(nameof(UndoCommandsString));
        }

        public void Redo()
        {
            if (CanRedo)
            {
                var redoCmd = RedoCommands.Pop();
                redoCommandsString.Pop();

                OnPropertyChanged(nameof(RedoCommands));
                OnPropertyChanged(nameof(redoCommandsString));

                AddUndoHistoryInternal(redoCmd);

                redoCmd.DoRedo();
            }
        }

        public void Undo()
        {
            if (CanUndo)
            {
                var undoCmd = UndoCommands.Pop(); 
                undoCommandsString.Pop();

                OnPropertyChanged(nameof(UndoCommands));
                OnPropertyChanged(nameof(undoCommandsString));

                AddRedoHistoryInternal(undoCmd);

                undoCmd.DoUndo();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
