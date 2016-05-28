# KCVDB.LogPublisher

```
> KCVDB.LogPublisher.exe [--delete] [--state-input 入力stateディレクトリへのパス] --input 入力logディレクトリへのパス --state-output 出力ディレクトリへのパス --output 出力logディレクトリへのパス
```
logディレクトリは1日分を指定する。  
2日分以上のlogを扱う場合は2回以上に分け、古いlogから順にコマンドを実行する。  
--state-outputで指定したディレクトリ内に現時点のセッションデータ等が記録される。  
--state-inputに前日出力した--state-ouputのパスを指定する。  
こうすることで、日をまたいだセッションの処理がなされる。  
--deleteを指定すると入力logを随時削除する。
