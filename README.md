# KCVDB.LogPublisher

```
> KCVDB.LogPublisher.exe [--delete] stateファイルへのパス 入力logディレクトリへのパス 出力logディレクトリへのパス
```
stateファイルはなければ作成される。  
logディレクトリは1日分を指定する。  
2日分以上のlogを扱う場合は2回以上に分け、古いlogから順にコマンドを実行する。  
--deleteを指定すると入力logを随時削除する。
