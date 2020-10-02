namespace CoalLang {
  public interface IVisitable {
    void Accept(IVisitor visitor);
  }
}