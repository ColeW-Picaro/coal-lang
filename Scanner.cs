using System.Collections.Generic;

class Scanner {
  private string source;
  private List<Token> tokens; 

  // string offsets fro keeping 
  // where we are
  private int start = 0;
  private int current = 0;
  private int line = 1;

  public Scanner (string source) {
    this.source = source;    
  }

  /*
    Creates a list of tokens from the
    input string
    @return tokens - the generated token list
  */
  public List<Token> scanTokens () {    

    while (!atEnd()) {
      start = current;
      scanToken();
    }

    this.tokens.Add(new Token(TokenType.EOF, ""));    
    return this.tokens;
  }

  /*
    Helper for scanTokens() to check if scanner is at the end
  */
  private bool atEnd () {
    return current >= source.Length;
  }

  /*
    Helper for scanTokens() to consume a token
  */
  private void scanToken () {    
    char c = advance();
    switch (c) {
      case '(': 
        addToken(TokenType.LEFT_PAREN);
        break;
      case ')':
        addToken(TokenType.RIGHT_PAREN);
        break;        
      default:
        System.Console.WriteLine("Error\n");
        break;
    }
  }

  private char advance () {
    ++current;
    return source[current - 1];
  }

  private void addToken (TokenType type) {
    tokens.Add (new Token(type, this.source.Substring(start, current)));
  }

}