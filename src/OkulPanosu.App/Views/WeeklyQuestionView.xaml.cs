using System.Windows;
using System.Windows.Controls;
using OkulPanosu.App.Services;
using OkulPanosu.Core.Data;

namespace OkulPanosu.App.Views;

public partial class WeeklyQuestionView : UserControl, IReloadablePage, IEmbeddableContentPage
{
    private WeeklyQuestion _question = new();

    public WeeklyQuestionView()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        if (AppServices.Data is not { } repo) return;
        _question = repo.Load().WeeklyQuestion;
        Rebuild();
    }

    public void Reload() => Load();

    private void Rebuild()
    {
        RiddlePanel.Children.Clear();
        RiddlePanel.Children.Add(EditorControls.LabeledMultilineTextBox("Soru", _question.Question, v => _question.Question = v));
        RiddlePanel.Children.Add(EditorControls.LabeledTextBox("Cevap", _question.Answer, v => _question.Answer = v));
        RiddlePanel.Children.Add(EditorControls.LabeledTextBox("İpucu (opsiyonel)", _question.Clue, v => _question.Clue = v));

        WordPanel.Children.Clear();
        WordPanel.Children.Add(EditorControls.LabeledTextBox("Kelime", _question.Word, v => _question.Word = v, 0));
        WordPanel.Children.Add(EditorControls.LabeledTextBox("Anlamı", _question.Meaning, v => _question.Meaning = v));
        WordPanel.Children.Add(EditorControls.LabeledMultilineTextBox("Örnek Cümle", _question.Example, v => _question.Example = v, 0));
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (AppServices.Data is not { } repo) return;
        repo.UpdateContent(data => data.WeeklyQuestion = _question);
        StatusText.Text = "Kaydedildi.";
        StatusText.Visibility = Visibility.Visible;
    }

    public void SetEmbedded() => SaveButton.Visibility = Visibility.Collapsed;

    public void SaveContent() => Save_Click(this, new RoutedEventArgs());
}
