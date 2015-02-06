--
-- Create Oracle Wallet and load Parature certificate and your own certificate
-- Next run following commands (update to use your values)
--
BEGIN
  DBMS_NETWORK_ACL_ADMIN.CREATE_ACL(
    acl         => 'wallet-acl.xml', 
    description => 'Wallet ACL',
    principal   => '<Schema Owner>',
    is_grant    => TRUE,
    privilege   => 'use-client-certificates');

  DBMS_NETWORK_ACL_ADMIN.ADD_PRIVILEGE(
    acl         => 'wallet-acl.xml', 
    principal   => '<Schema Owner>',
    is_grant    => TRUE,
    privilege   => 'use-passwords');

  DBMS_NETWORK_ACL_ADMIN.ASSIGN_WALLET_ACL(
    acl         => 'wallet-acl.xml', 
    wallet_path => 'file:<Wallet file path>');
END;
/
--
-- Sample Code: (replace all xxxxxxxxx with your values)
--
DECLARE

  http_request VARCHAR2(30000);
  http_respond VARCHAR2(30000);
  http_req utl_http.req;
  http_resp utl_http.resp; 
  wfws_url       VARCHAR2(250) := 'https://sso-mutual-auth.parature.com/ext/ref/dropoff';
  refID             VARCHAR2(250);
  refIDStartIndex   NUMBER(3);

BEGIN 
  --
  -- Step One
  --
  http_request := '
  {
   "subject": "xxxxxxxxx",
   "payload": {
    "sessEmail": "xxxxxxxxx@xxxxxxxxx",
    "cEmail": "xxxxxxxxx@xxxxxxxxx",
    "cUname": "xxxxxxxxx",
    "deptID": "xxxxxxxxx",
    }
  }';
  --
  UTL_HTTP.set_wallet('file:<Wallet file path>', 'xxxxxxxxx');
  --
  http_req:= utl_http.begin_request(wfws_url, 'GET', 'HTTP/1.1');
  utl_http.set_header(http_req, 'Content-Type', 'application/json'); 
  utl_http.set_header(http_req, 'Content-Length', length(http_request));
  utl_http.set_header(http_req, 'ping.instanceId', 'xxxxxxxxx');
  utl_http.write_text(http_req, http_request);
  --
  http_resp:= utl_http.get_response(http_req);
  utl_http.read_text(http_resp, http_respond);
  utl_http.end_response(http_resp);
  --
  -- Step Two
  --
  refIDStartIndex := INSTR(http_respond,'"REF"')+7;
  refID := SUBSTR(http_respond, refIDStartIndex, INSTR(http_respond, '"', refIDStartIndex)-refIDStartIndex);
  --
  -- Connect to Parature
  --
  htp.p('<html><body bgcolor="#EDEBE6">');
  htp.p('<br><br><br><br><br><center><font size=4 color=RED>Please wait</font></center>');
  htp.p('<form ACTION="https://xxxxxxxxx.parature.com/ics/support/security2.asp" METHOD="post" NAME="UserLogin">');
  htp.p('<input TYPE="hidden" NAME="refID" VALUE="' || refID || '">');
  htp.p('<input TYPE="hidden" NAME="instanceID" VALUE="xxxxxxxxx">');
  htp.p('</form>');
  htp.p('<SCRIPT LANGUAGE="JavaScript">');
  htp.p('<!--  Hide from non-JavaScript enabled browsers ');
  htp.p('window.document.UserLogin.submit();');
  htp.p('// end script hiding from old browsers -->');
  htp.p('</SCRIPT>');
  htp.p('</body></html>');
END;

  
  