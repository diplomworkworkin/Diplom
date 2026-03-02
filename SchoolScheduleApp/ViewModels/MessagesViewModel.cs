using SchoolScheduleApp.Data.Entites;
using SchoolScheduleApp.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Net.Http;
using System.Threading.Tasks;

namespace SchoolScheduleApp.ViewModels
{
    public enum MessageCategory
    {
        LessonReplacement,
        ScheduleChange
    }

    public enum ReplacementMode
    {
        AddMyLesson,
        ReplaceMyLesson
    }

    public enum MessageStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class MessageCategoryOption
    {
        public MessageCategory Value { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class ReplacementModeOption
    {
        public ReplacementMode Value { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class DayOption
    {
        public int Value { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class TeacherClassOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TeacherOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ConversationItemViewModel
    {
        public Guid Id { get; set; }
        public int TeacherId { get; set; }
        public string Header { get; set; } = string.Empty;
        public string LastMessagePreview { get; set; } = string.Empty;
        public string UpdatedAtText { get; set; } = string.Empty;
        public MessageStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public Brush StatusBrush { get; set; } = Brushes.Gray;

        public MessageCategory Category { get; set; }
        public string CategoryText { get; set; } = string.Empty;

        public ReplacementMode? ReplacementMode { get; set; }
        public string ReplacementModeText { get; set; } = "—";

        public int? ReplacementTeacherId { get; set; }
        public string? ReplacementTeacherName { get; set; }

        public int? TargetClassId { get; set; }
        public string? TargetClassName { get; set; }
        public int? TargetDayOfWeek { get; set; }
        public int? TargetLessonIndex { get; set; }
    }

    public class ChatMessageViewModel
    {
        public string Text { get; set; } = string.Empty;
        public string Meta { get; set; } = string.Empty;
        public HorizontalAlignment Alignment { get; set; }
        public Brush BubbleBackground { get; set; } = Brushes.Transparent;
        public Brush BubbleForeground { get; set; } = Brushes.White;
    }

    public class MessagesViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly Brush _incomingBubble = (Brush)new BrushConverter().ConvertFrom("#2D4F6E");
        private readonly Brush _outgoingBubble = (Brush)new BrushConverter().ConvertFrom("#0EA5E9");

        private ConversationItemViewModel? _selectedConversation;
        private string _chatInput = string.Empty;
        private string _newRequestMessage = string.Empty;
        private bool _isComposerOpen;

        private MessageCategoryOption? _selectedCategoryOption;
        private ReplacementModeOption? _selectedReplacementModeOption;
        private TeacherClassOption? _selectedClassOption;
        private DayOption? _selectedDayOption;
        private TeacherOption? _selectedReplacementTeacherOption;
        private int _lessonIndex = 1;

        public bool IsAdmin => UserSession.CurrentUser?.Role == UserRole.Admin;
        public bool IsTeacher => UserSession.CurrentUser?.Role == UserRole.Teacher;

        public bool IsLessonReplacementSelected => SelectedCategoryOption?.Value == MessageCategory.LessonReplacement;
        public bool IsReplaceByOtherTeacher => SelectedReplacementModeOption?.Value == ReplacementMode.ReplaceMyLesson;

        public ObservableCollection<ConversationItemViewModel> Conversations { get; } = new();
        public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = new();

        public IReadOnlyList<MessageCategoryOption> Categories { get; } = new[]
        {
            new MessageCategoryOption { Value = MessageCategory.LessonReplacement, Title = "Замена урока" },
            new MessageCategoryOption { Value = MessageCategory.ScheduleChange, Title = "Внести изменения" }
        };

        public IReadOnlyList<ReplacementModeOption> ReplacementModes { get; } = new[]
        {
            new ReplacementModeOption { Value = ReplacementMode.AddMyLesson, Title = "Поставить мой урок" },
            new ReplacementModeOption { Value = ReplacementMode.ReplaceMyLesson, Title = "Заменить мой урок другим учителем" }
        };

        public IReadOnlyList<DayOption> Days { get; } = new[]
        {
            new DayOption { Value = 1, Title = "Понедельник" },
            new DayOption { Value = 2, Title = "Вторник" },
            new DayOption { Value = 3, Title = "Среда" },
            new DayOption { Value = 4, Title = "Четверг" },
            new DayOption { Value = 5, Title = "Пятница" },
            new DayOption { Value = 6, Title = "Суббота" }
        };

        public ObservableCollection<TeacherClassOption> TeacherClasses { get; } = new();
        public ObservableCollection<TeacherOption> ReplacementTeachers { get; } = new();

        public ConversationItemViewModel? SelectedConversation
        {
            get => _selectedConversation;
            set
            {
                _selectedConversation = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSendInChat));
                OnPropertyChanged(nameof(CanModerate));
                _ = LoadSelectedConversationMessages();
            }
        }

        public string ChatInput
        {
            get => _chatInput;
            set { _chatInput = value; OnPropertyChanged(); }
        }

        public string NewRequestMessage
        {
            get => _newRequestMessage;
            set { _newRequestMessage = value; OnPropertyChanged(); }
        }

        public bool IsComposerOpen
        {
            get => _isComposerOpen;
            set { _isComposerOpen = value; OnPropertyChanged(); }
        }

        public MessageCategoryOption? SelectedCategoryOption
        {
            get => _selectedCategoryOption;
            set
            {
                _selectedCategoryOption = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLessonReplacementSelected));
                OnPropertyChanged(nameof(IsReplaceByOtherTeacher));
            }
        }

        public ReplacementModeOption? SelectedReplacementModeOption
        {
            get => _selectedReplacementModeOption;
            set
            {
                _selectedReplacementModeOption = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReplaceByOtherTeacher));
            }
        }

        public TeacherClassOption? SelectedClassOption
        {
            get => _selectedClassOption;
            set { _selectedClassOption = value; OnPropertyChanged(); }
        }

        public DayOption? SelectedDayOption
        {
            get => _selectedDayOption;
            set { _selectedDayOption = value; OnPropertyChanged(); }
        }

        public TeacherOption? SelectedReplacementTeacherOption
        {
            get => _selectedReplacementTeacherOption;
            set { _selectedReplacementTeacherOption = value; OnPropertyChanged(); }
        }

        public int LessonIndex
        {
            get => _lessonIndex;
            set { _lessonIndex = value; OnPropertyChanged(); }
        }

        public bool CanSendInChat => SelectedConversation != null;
        public bool CanModerate => IsAdmin && SelectedConversation != null && SelectedConversation.Status == MessageStatus.Pending;

        public RelayCommand RefreshCommand { get; }
        public RelayCommand ToggleComposerCommand { get; }
        public RelayCommand CreateRequestCommand { get; }
        public RelayCommand SendChatMessageCommand { get; }
        public RelayCommand ApproveCommand { get; }
        public RelayCommand RejectCommand { get; }

        public MessagesViewModel()
        {
            _apiClient = new ApiClient();
            RefreshCommand = new RelayCommand(async (param) => await LoadConversations());
            ToggleComposerCommand = new RelayCommand(_ => IsComposerOpen = !IsComposerOpen);
            CreateRequestCommand = new RelayCommand(async (param) => await CreateRequest());
            SendChatMessageCommand = new RelayCommand(async (param) => await SendChatMessage());
            ApproveCommand = new RelayCommand(async (param) => await ProcessRequest(true));
            RejectCommand = new RelayCommand(async (param) => await ProcessRequest(false));

            SelectedCategoryOption = Categories.FirstOrDefault();
            SelectedReplacementModeOption = ReplacementModes.FirstOrDefault();
            SelectedDayOption = Days.FirstOrDefault(x => x.Value == 5) ?? Days.FirstOrDefault();

            _ = LoadTeacherClasses();
            _ = LoadReplacementTeachers();
            _ = LoadConversations();
        }

        private async Task LoadTeacherClasses()
        {
            TeacherClasses.Clear();

            if (!IsTeacher || UserSession.CurrentUser?.TeacherId == null)
            {
                return;
            }

            var teacherId = UserSession.CurrentUser.TeacherId.Value;

            try
            {
                var lessons = await _apiClient.GetLessonsAsync(teacherId: teacherId);
                var classes = lessons
                    .Where(x => x.AcademicClassId.HasValue)
                    .Select(x => new { AcademicClassId = x.AcademicClassId.Value, x.AcademicClass?.Name })
                    .Distinct()
                    .ToList();

                foreach (var item in classes.OrderBy(x => x.Name))
                {
                    TeacherClasses.Add(new TeacherClassOption { Id = item.AcademicClassId, Name = item.Name });
                }

                SelectedClassOption = TeacherClasses.FirstOrDefault();
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки классов учителя из API.", ex);
                ToastService.Show("Ошибка загрузки классов учителя. Проверьте подключение к API.", "Ошибка");
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки классов учителя.", ex);
                ToastService.Show("Произошла ошибка при загрузке классов учителя.", "Ошибка");
            }
        }

        private async Task LoadReplacementTeachers()
        {
            ReplacementTeachers.Clear();

            try
            {
                var teachers = await _apiClient.GetTeachersAsync();
                foreach (var teacher in teachers.OrderBy(x => x.FullName))
                {
                    ReplacementTeachers.Add(new TeacherOption { Id = teacher.Id, Name = teacher.FullName });
                }

                SelectedReplacementTeacherOption = ReplacementTeachers.FirstOrDefault();
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки учителей для замены из API.", ex);
                ToastService.Show("Ошибка загрузки учителей для замены. Проверьте подключение к API.", "Ошибка");
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки учителей для замены.", ex);
                ToastService.Show("Произошла ошибка при загрузке учителей для замены.", "Ошибка");
            }
        }

        private async Task LoadConversations()
        {
            Conversations.Clear();
            ChatMessages.Clear();
            SelectedConversation = null;

            try
            {
                // TODO: Implement API endpoint for conversations
                // For now, mock data or skip if no API yet
                // var conversations = await _apiClient.GetConversationsAsync();
                // foreach (var conv in conversations)
                // {
                //     Conversations.Add(MapToConversationItemViewModel(conv));
                // }
                ToastService.Show("Функционал загрузки бесед пока не реализован через API.", "Информация");
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки бесед из API.", ex);
                ToastService.Show("Ошибка загрузки бесед. Проверьте подключение к API.", "Ошибка");
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки бесед.", ex);
                ToastService.Show("Произошла ошибка при загрузке бесед.", "Ошибка");
            }
        }

        private async Task LoadSelectedConversationMessages()
        {
            ChatMessages.Clear();
            if (SelectedConversation == null) return;

            try
            {
                // TODO: Implement API endpoint for chat messages
                // For now, mock data or skip if no API yet
                // var messages = await _apiClient.GetChatMessagesAsync(SelectedConversation.Id);
                // foreach (var msg in messages)
                // {
                //     ChatMessages.Add(MapToChatMessageViewModel(msg));
                // }
                ToastService.Show("Функционал загрузки сообщений беседы пока не реализован через API.", "Информация");
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError($"Ошибка загрузки сообщений для беседы {SelectedConversation.Id} из API.", ex);
                ToastService.Show("Ошибка загрузки сообщений. Проверьте подключение к API.", "Ошибка");
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Ошибка загрузки сообщений для беседы {SelectedConversation.Id}.", ex);
                ToastService.Show("Произошла ошибка при загрузке сообщений.", "Ошибка");
            }
        }

        private async Task CreateRequest()
        {
            if (UserSession.CurrentUser == null || UserSession.CurrentUser.TeacherId == null)
            {
                ToastService.Show("Для создания запроса необходимо быть авторизованным учителем.", "Ошибка");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewRequestMessage))
            {
                ToastService.Show("Сообщение запроса не может быть пустым.", "Ошибка");
                return;
            }

            if (SelectedCategoryOption == null)
            {
                ToastService.Show("Выберите категорию запроса.", "Ошибка");
                return;
            }

            // TODO: Implement API endpoint for creating requests
            // For now, mock data or skip if no API yet
            ToastService.Show("Функционал создания запросов пока не реализован через API.", "Информация");
            IsComposerOpen = false;
            NewRequestMessage = string.Empty;
            // await LoadConversations();
        }

        private async Task SendChatMessage()
        {
            if (SelectedConversation == null || string.IsNullOrWhiteSpace(ChatInput))
            {
                return;
            }

            // TODO: Implement API endpoint for sending chat messages
            // For now, mock data or skip if no API yet
            ToastService.Show("Функционал отправки сообщений пока не реализован через API.", "Информация");
            ChatInput = string.Empty;
            // await LoadSelectedConversationMessages();
        }

        private async Task ProcessRequest(bool approve)
        {
            if (SelectedConversation == null || !IsAdmin)
            {
                return;
            }

            // TODO: Implement API endpoint for approving/rejecting requests
            // For now, mock data or skip if no API yet
            ToastService.Show($"Функционал {(approve ? "одобрения" : "отклонения")} запросов пока не реализован через API.", "Информация");
            // await LoadConversations();
        }
    }
}
