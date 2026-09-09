using System.Windows;
using System.Windows.Controls;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views.Modules;

public partial class WeeklyQuestionModuleView : UserControl
{
    public WeeklyQuestionModuleView(BoardModule module)
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        var q = AppServices.Data?.Load().WeeklyQuestion;

        if (q is null || string.IsNullOrWhiteSpace(q.Question))
        {
            ContentPanel.Visibility = Visibility.Collapsed;
            EmptyText.Text = "Haftanın sorusu tanımlanmamış";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        QuestionText.Text = $"❓ {q.Question}";
        AnswerText.Text = $"Cevap: {q.Answer}";
        WordText.Text = q.Word;
        MeaningText.Text = q.Meaning;
        ExampleText.Text = string.IsNullOrWhiteSpace(q.Example) ? "" : $"“{q.Example}”";

        ContentPanel.Visibility = Visibility.Visible;
        EmptyText.Visibility = Visibility.Collapsed;
    }
}
