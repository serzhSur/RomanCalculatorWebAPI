namespace RomanCalculatorWeb.models
{
    public interface ICalculateService
    {
        public string Errors {  get; }
        string Calculate(string expression);
    }
}
